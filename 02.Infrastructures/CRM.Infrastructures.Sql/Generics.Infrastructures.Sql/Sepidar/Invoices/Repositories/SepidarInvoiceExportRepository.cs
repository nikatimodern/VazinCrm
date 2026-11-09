using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Models;
using CRM.Core.Domain.Sepidar.Invoices.Repositories;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Sepidar.Invoices.Repositories;

public abstract class SepidarInvoiceExportRepository(SepidarContextDbBase context) : ISepidarInvoiceExportRepository {
    public async Task<SepidarInvoiceExportPreview> PreviewAsync(Factor factor, PreFactor preFactor, int quotationId, CancellationToken ct = default)
    {
        var quotation = await context.Quotations.AsNoTracking()
                            .Include(x => x.QuotationItems)
                            .SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct)
                        ?? throw new InvalidOperationException($"پیش‌فاکتور نگاشت‌شده در سپیدار پیدا نشد (شناسه: {quotationId}).");

        if (factor.PfactorID != preFactor.Id)
            throw new InvalidOperationException("فاکتور با پیش‌فاکتور انتخاب‌شده مطابقت ندارد.");

        if (factor.ParentReturnedId.HasValue || factor.Returned)
            throw new InvalidOperationException($"صدور فاکتور برگشتی پشتیبانی نمی‌شود (شماره فاکتور: {factor.SerialNumber}).");

        if (factor.SerialNumber <= 0 || !factor.DateMiladi.HasValue)
            throw new InvalidOperationException("شماره یا تاریخ فاکتور CRM معتبر نیست.");

        var fiscalYear = await context.FiscalYears.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FiscalYearId == quotation.FiscalYearRef, ct);

        if (fiscalYear == null || factor.DateMiladi.Value.Date < fiscalYear.StartDate.Date
                               || factor.DateMiladi.Value.Date > fiscalYear.EndDate.Date)
            throw new InvalidOperationException($"تاریخ فاکتور ({factor.DateMiladi:yyyy/MM/dd}) خارج از سال مالی پیش‌فاکتور سپیدار است.");

        if (quotation.CustomerPartyRef <= 0 || quotation.SaleTypeRef <= 0)
            throw new InvalidOperationException("مشتری یا نوع فروش پیش‌فاکتور سپیدار نامعتبر است.");

        var preFactorItems = (preFactor.PreFactorItems ?? Array.Empty<PreFactorItem>())
            .Where(x => !x.IsDeleted).ToList();
        var factorItems = (factor.FactorItems ?? Array.Empty<FactorItem>())
            .Where(x => !x.IsDeleted).OrderBy(x => x.Row).ToList();

        if (preFactorItems.Count == 0 || factorItems.Count == 0)
            throw new InvalidOperationException(
                $"پیش‌فاکتور یا فاکتور ردیف معتبر ندارد (پیش‌فاکتور: {preFactorItems.Count}، فاکتور: {factorItems.Count}).");

        if (factorItems.Any(x => !x.PfItemId.HasValue)
            || factorItems.Select(x => x.PfItemId).Distinct().Count() != factorItems.Count)
            throw new InvalidOperationException("هر ردیف فاکتور باید به یک ردیف یکتای پیش‌فاکتور CRM متصل باشد.");

        var itemsById = preFactorItems.ToDictionary(x => x.Id);
        var quotationItemsByRow = quotation.QuotationItems.ToDictionary(x => x.RowID);
        var quotationItemIds = new HashSet<int>();
        var lines = new List<SepidarInvoiceExportItemPreview>();

        foreach (var factorItem in factorItems)
        {
            if (!itemsById.TryGetValue(factorItem.PfItemId!.Value, out var sourceItem)
                || !quotationItemsByRow.TryGetValue(sourceItem.Row, out var quotationItem)
                || !quotationItemIds.Add(quotationItem.QuotationItemID))
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: کالای متناظر در پیش‌فاکتور سپیدار مشخص نیست.");

            if (quotationItem.ItemRef <= 0 || quotationItem.StockRef <= 0)
                throw new InvalidOperationException($"ردیف {factorItem.Row}: کالا یا انبار پیش‌فاکتور سپیدار نامعتبر است.");

            // The older quotation exporter assigned RowID by collection order.
            // Verify the original quantity and fee before trusting the row correspondence.
            if (!decimal.TryParse(sourceItem.WeightCount, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var sourceQuantity)
                || sourceQuantity != quotationItem.Quantity
                || sourceItem.UnitAmount != quotationItem.Fee)
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: تعداد یا قیمت CRM با سپیدار متفاوت است (تعداد: {sourceItem.WeightCount}/{quotationItem.Quantity:N3}، قیمت: {sourceItem.UnitAmount:N0}/{quotationItem.Fee:N0}).");

            if (!decimal.TryParse(factorItem.WeightCount, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                throw new InvalidOperationException($"ردیف {factorItem.Row}: مقدار فاکتور نامعتبر است ({factorItem.WeightCount}).");

            // Source quotation can contain more quantity than this invoice (partial invoicing).
            if (quantity > quotationItem.Quantity)
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: مقدار فاکتور ({quantity:N3}) از پیش‌فاکتور سپیدار ({quotationItem.Quantity:N3}) بیشتر است.");

            var price = Math.Round(quantity * factorItem.UnitAmount);
            var discount = factorItem.Discount;
            var tax = factorItem.TaxAmount;
            if (price < 0 || discount < 0 || discount > price || tax < 0
                || factorItem.TotalAmount != price - discount + tax)
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: مبلغ فاکتور ({factorItem.TotalAmount:N0}) با مبلغ محاسبه‌شده ({price - discount + tax:N0}) برابر نیست.");

            lines.Add(new SepidarInvoiceExportItemPreview {
                RowId = lines.Count + 1,
                QuotationItemId = quotationItem.QuotationItemID,
                ItemRef = quotationItem.ItemRef,
                CrmProductTitle = $"{factorItem.ProductSmallName} {factorItem.Product}".Trim(),
                ItemTitle = string.Empty, // Resolved from the linked Sepidar quotation item by SepidarService.
                StockRef = quotationItem.StockRef,
                StockTitle = string.Empty, // Filled by SepidarService from the mapped StockRef.
                Quantity = quantity,
                Fee = factorItem.UnitAmount,
                Price = price,
                Discount = discount,
                Tax = tax,
                NetPrice = factorItem.TotalAmount
            });
        }

        if (lines.Sum(x => x.Price) != factor.SumTotalAmount
            || lines.Sum(x => x.Discount) != factor.Discount
            || lines.Sum(x => x.Tax) != factor.TotalTax
            || lines.Sum(x => x.NetPrice) != factor.PaymentAmount)
            throw new InvalidOperationException(
                $"مبالغ ردیف‌های فاکتور با جمع کل CRM برابر نیست (جمع ردیف‌ها: {lines.Sum(x => x.NetPrice):N0}، مبلغ نهایی: {factor.PaymentAmount:N0}).");

        /*var lineIds = lines.Select(x => x.QuotationItemId).ToArray();

        var previousQuantities = await context.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.State == 1)
            .SelectMany(invoice => invoice.InvoiceItems)
            .Where(item => item.QuotationItemRef.HasValue && lineIds.Contains(item.QuotationItemRef.Value))
            .GroupBy(item => item.QuotationItemRef.Value)
            .Select(group => new {
                QuotationItemId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToListAsync(ct);

        var previousByItem = previousQuantities.ToDictionary(x => x.QuotationItemId, x => x.Quantity);
        foreach (var line in lines)
        {
            var quotedQuantity = quotationItemsByRow.Values
                .Single(x => x.QuotationItemID == line.QuotationItemId).Quantity;

            var previousQuantity = previousByItem.GetValueOrDefault(line.QuotationItemId);
            if (line.Quantity + previousQuantity > quotedQuantity)
                throw new InvalidOperationException(
                    $"ردیف {line.RowId}: مجموع فاکتورهای صادرشده ({line.Quantity + previousQuantity:N3}) از تعداد پیش‌فاکتور سپیدار ({quotedQuantity:N3}) بیشتر می‌شود.");
        }*/

        return new SepidarInvoiceExportPreview {
            QuotationId = quotationId,
            QuotationNumber = quotation.Number,
            InvoiceNumber = null, // Assigned at creation from the last number in Sepidar fiscal year.
            InvoiceDate = factor.DateMiladi.Value,
            InvoiceDateText = factor.Date,
            CustomerName = quotation.CustomerRealName,
            SaleTypeRef = quotation.SaleTypeRef,
            FiscalYearRef = quotation.FiscalYearRef,
            Price = factor.SumTotalAmount,
            Discount = factor.Discount,
            Tax = factor.TotalTax,
            NetPrice = factor.PaymentAmount,
            Items = lines
        };
    }
}

public sealed class Sepidar1InvoiceExportRepository(Sepidar1ContextDb db) : SepidarInvoiceExportRepository(db) { }

public sealed class Sepidar2InvoiceExportRepository(Sepidar2ContextDb db) : SepidarInvoiceExportRepository(db) { }