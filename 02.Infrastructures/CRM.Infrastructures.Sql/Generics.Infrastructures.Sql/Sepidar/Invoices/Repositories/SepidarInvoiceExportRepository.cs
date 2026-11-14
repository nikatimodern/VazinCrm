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
    // Preserve significant decimal digits and grouping while hiding trailing fractional zeros.
    private static string FormatErrorNumber(decimal value)
        => value.ToString("#,0.############################", CultureInfo.InvariantCulture);

    private static string FormatErrorNumber(string? value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? FormatErrorNumber(number)
            : value ?? string.Empty;

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

        var invoiceDate = factor.DateMiladi.Value;
        if (fiscalYear == null || invoiceDate.Date < fiscalYear.StartDate.Date
                               || invoiceDate.Date > fiscalYear.EndDate.Date)
            throw new InvalidOperationException(
                $"تاریخ فاکتور CRM ({factor.Date}) خارج از سال مالی پیش‌فاکتور سپیدار است.");

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

            // Sepidar's quotation supplies the item/stock mapping only. The CRM
            // invoice is authoritative for quantity, unit price, discount, tax
            // and net amount; they can differ from the earlier quotation.
            if (!decimal.TryParse(factorItem.WeightCount, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: مقدار فاکتور نامعتبر است ({FormatErrorNumber(factorItem.WeightCount)}).");

            var fee = factorItem.UnitAmount;
            var discount = factorItem.Discount;
            var tax = factorItem.TaxAmount;
            var netPrice = factorItem.TotalAmount;

            // CRM stores the final line amount after discount and tax.
            // Reconstruct the before-discount/tax price from CRM amounts,
            // without taking the quotation price or recalculating CRM's rounding.
            var price = netPrice + discount - tax;
            if (fee < 0 || price < 0 || netPrice < 0 || discount < 0 ||
                discount > price || tax < 0)
                throw new InvalidOperationException(
                    $"ردیف {factorItem.Row}: مبلغ، قیمت، تخفیف یا مالیات فاکتور CRM نامعتبر است.");

            lines.Add(new SepidarInvoiceExportItemPreview {
                RowId = lines.Count + 1,
                QuotationItemId = quotationItem.QuotationItemID,
                ItemRef = quotationItem.ItemRef,
                CrmProductTitle = $"{factorItem.ProductSmallName} {factorItem.Product}".Trim(),
                ItemTitle = string.Empty, // Resolved from the linked Sepidar quotation item by SepidarService.
                StockRef = quotationItem.StockRef,
                StockTitle = string.Empty, // Filled by SepidarService from the mapped StockRef.
                Quantity = quantity,
                Fee = fee,
                Price = price,
                Discount = discount,
                Tax = tax,
                NetPrice = netPrice
            });
        }

        if (lines.Sum(x => x.Price) != factor.SumTotalAmount
            || lines.Sum(x => x.Discount) != factor.Discount
            || lines.Sum(x => x.Tax) != factor.TotalTax
            || lines.Sum(x => x.NetPrice) != factor.PaymentAmount)
            throw new InvalidOperationException(
                $"مبالغ ردیف‌های فاکتور با جمع کل CRM برابر نیست (جمع ردیف‌ها: {FormatErrorNumber(lines.Sum(x => x.NetPrice))}، مبلغ نهایی: {FormatErrorNumber(factor.PaymentAmount)}).");

        return new SepidarInvoiceExportPreview {
            QuotationId = quotationId,
            QuotationNumber = quotation.Number,
            InvoiceNumber = null, // Assigned at creation from the last number in Sepidar fiscal year.
            InvoiceDate = invoiceDate,
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