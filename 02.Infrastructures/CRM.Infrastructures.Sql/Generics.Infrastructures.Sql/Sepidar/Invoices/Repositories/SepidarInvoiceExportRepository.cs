using System;
using System.Collections.Generic;
using System.Data;
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

public abstract class SepidarInvoiceExportRepository(SepidarContextDbBase context)
    : ISepidarInvoiceExportRepository
{
    public async Task<SepidarInvoiceExportPreview> PreviewAsync(
        Factor factor, PreFactor preFactor, int quotationId, CancellationToken ct = default)
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
            throw new InvalidOperationException($"پیش‌فاکتور یا فاکتور ردیف معتبر ندارد (پیش‌فاکتور: {preFactorItems.Count}، فاکتور: {factorItems.Count}).");

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
                throw new InvalidOperationException($"ردیف {factorItem.Row}: مقدار فاکتور ({quantity:N3}) از پیش‌فاکتور سپیدار ({quotationItem.Quantity:N3}) بیشتر است.");

            var price = Math.Round(quantity * factorItem.UnitAmount);
            var discount = factorItem.Discount;
            var tax = factorItem.TaxAmount;
            if (price < 0 || discount < 0 || discount > price || tax < 0
                || factorItem.TotalAmount != price - discount + tax)
                throw new InvalidOperationException($"ردیف {factorItem.Row}: مبلغ فاکتور ({factorItem.TotalAmount:N0}) با مبلغ محاسبه‌شده ({price - discount + tax:N0}) برابر نیست.");

            lines.Add(new SepidarInvoiceExportItemPreview {
                RowId = lines.Count + 1,
                QuotationItemId = quotationItem.QuotationItemID,
                ItemRef = quotationItem.ItemRef,
                CrmProductTitle = $"{factorItem.ProductSmallName} {factorItem.Product}".Trim(),
                ItemTitle = string.Empty, // Resolved from the linked Sepidar quotation item by SepidarService.
                StockRef = quotationItem.StockRef,
                StockTitle = quotationItem.StockRef.ToString(CultureInfo.InvariantCulture),
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
            throw new InvalidOperationException($"مبالغ ردیف‌های فاکتور با جمع کل CRM برابر نیست (جمع ردیف‌ها: {lines.Sum(x => x.NetPrice):N0}، مبلغ نهایی: {factor.PaymentAmount:N0}).");

        var lineIds = lines.Select(x => x.QuotationItemId).ToArray();
        var previousQuantities = await context.SalesInvoiceItems.AsNoTracking()
            .Where(x => lineIds.Contains(x.QuotationItemRef))
            .Join(context.SalesInvoices.AsNoTracking().Where(x => x.State == 1
                    && x.Guid != factor.Id),
                item => item.InvoiceRef, invoice => invoice.InvoiceId,
                (item, invoice) => new { item.QuotationItemRef, item.Quantity })
            .GroupBy(x => x.QuotationItemRef)
            .Select(x => new { QuotationItemId = x.Key, Quantity = x.Sum(y => y.Quantity) })
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
        }

        var stockIds = lines.Select(x => x.StockRef).Distinct().ToArray();
        var stocks = await context.Stocks.AsNoTracking()
            .Where(x => stockIds.Contains(x.StockID))
            .Select(x => new { x.StockID, x.Title })
            .ToDictionaryAsync(x => x.StockID, x => x.Title, ct);
        foreach (var line in lines)
            line.StockTitle = stocks.GetValueOrDefault(line.StockRef)
                ?? line.StockRef.ToString(CultureInfo.InvariantCulture);

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

    public async Task<int> CreateAsync(
        Factor factor, PreFactor preFactor, int quotationId, CancellationToken ct = default)
    {
        var preview = await PreviewAsync(factor, preFactor, quotationId, ct);
        // Invoice numbers belong to Sepidar, not CRM. A stable CRM factor GUID is
        // used to identify safe retries if Sepidar insertion succeeded before CRM mapping.
        var existing = await context.SalesInvoices.AsNoTracking()
            .Where(x => x.Guid == factor.Id)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            if (existing.Count != 1)
                throw new InvalidOperationException("بیش از یک فاکتور سپیدار با شناسه این فاکتور CRM وجود دارد.");

            var prior = existing[0];
            var priorItems = await context.SalesInvoiceItems.AsNoTracking()
                .Where(x => x.InvoiceRef == prior.InvoiceId).ToListAsync(ct);
            var same = prior.QuotationRef == quotationId
                && prior.FiscalYearRef == preview.FiscalYearRef
                && prior.CustomerPartyRef > 0
                && prior.Price == preview.Price
                && prior.Discount == preview.Discount
                && prior.Tax == preview.Tax
                && prior.NetPriceInBaseCurrency == preview.NetPrice
                && priorItems.Count == preview.Items.Count
                && preview.Items.All(line => priorItems.Any(item =>
                    item.QuotationItemRef == line.QuotationItemId
                    && item.Quantity == line.Quantity
                    && item.Fee == line.Fee
                    && item.Price == line.Price
                    && item.Discount == line.Discount
                    && item.Tax == line.Tax));
            if (same)
                return prior.InvoiceId;

            throw new InvalidOperationException(
                "این فاکتور CRM قبلاً با اطلاعات متفاوت در سپیدار ثبت شده است.");
        }

        var quotation = await context.Quotations.AsNoTracking()
            .SingleAsync(x => x.QuotationId == quotationId, ct);

        // SLRef is an accounting account reference. Never hard-code it from a screenshot.
        // Only use the existing, unambiguous setup for this customer and sale type.
        var accounts = await context.SalesInvoices.AsNoTracking()
            .Where(x => x.CustomerPartyRef == quotation.CustomerPartyRef
                && x.SaleTypeRef == quotation.SaleTypeRef
                && x.FiscalYearRef == quotation.FiscalYearRef
                && x.CurrencyRef == quotation.CurrencyRef)
            .Select(x => x.SLRef).Distinct().Take(2).ToListAsync(ct);

        if (accounts.Count != 1 || accounts[0] <= 0)
            throw new InvalidOperationException(
                "حساب فروش (SLRef) برای این مشتری و نوع فروش در سپیدار به شکل یکتا مشخص نیست؛ ابتدا تنظیم حساب را بررسی کنید.");

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        try
        {
            // Recheck the stable identity and allocate the number within the same
            // serializable transaction to prevent duplicate invoice numbering.
            if (await context.SalesInvoices.AsNoTracking()
                    .AnyAsync(x => x.Guid == factor.Id, ct))
                throw new InvalidOperationException("فاکتور CRM در این فاصله در سپیدار ثبت شده است؛ دوباره بررسی کنید.");

            var invoiceNumber = await context.GetNextInvoiceNumberAsync(preview.FiscalYearRef, ct);
            var invoiceId = await context.GetNextIdAsync("SLS.Invoice", ct: ct);
            var lastItemId = await context.GetNextIdAsync("SLS.InvoiceItem", preview.Items.Count, ct);
            var firstItemId = lastItemId - preview.Items.Count + 1;
            var now = DateTime.Now;

            var invoice = new SalesInvoiceRecord {
                InvoiceId = invoiceId,
                QuotationRef = quotation.QuotationId,
                CustomerPartyRef = quotation.CustomerPartyRef,
                CustomerRealName = quotation.CustomerRealName,
                CustomerRealName_En = quotation.CustomerRealName_En,
                SaleTypeRef = quotation.SaleTypeRef,
                PartyAddressRef = quotation.PartyAddressRef,
                Number = invoiceNumber,
                Date = factor.DateMiladi!.Value,
                CurrencyRef = quotation.CurrencyRef,
                SLRef = accounts[0],
                DeliveryLocationRef = quotation.DeliveryLocationRef,
                State = 1, // Sample approved invoice: active
                Price = preview.Price,
                PriceInBaseCurrency = preview.Price,
                Discount = preview.Discount,
                DiscountInBaseCurrency = preview.Discount,
                Addition = 0,
                AdditionInBaseCurrency = 0,
                Tax = preview.Tax,
                TaxInBaseCurrency = preview.Tax,
                Duty = 0,
                DutyInBaseCurrency = 0,
                NetPriceInBaseCurrency = preview.NetPrice,
                Rate = quotation.Rate,
                Version = 1,
                Creator = quotation.Creator,
                CreationDate = now,
                LastModifier = quotation.Creator,
                LastModificationDate = now,
                FiscalYearRef = quotation.FiscalYearRef,
                VoucherRef = null,
                BaseOnInventoryDelivery = false,
                OrderRef = null,
                ShouldControlCustomerCredit = true,
                Guid = factor.Id, // Stable idempotency key for retries.
                AdditionFactor_VatEffective = 0,
                AdditionFactorInBaseCurrency_VatEffective = 0,
                AdditionFactor_VatIneffective = 0,
                AdditionFactorInBaseCurrency_VatIneffective = 0,
                AgreementRef = null,
                TaxPayerBillIssueDateTime = factor.DateMiladi,
                SettlementType = 1, // Sample approved invoice: default settlement
                Description = null,
                SignatureRef = null
            };

            context.SalesInvoices.Add(invoice);
            for (var i = 0; i < preview.Items.Count; i++)
            {
                var item = preview.Items[i];
                context.SalesInvoiceItems.Add(new SalesInvoiceItemRecord {
                    InvoiceItemID = firstItemId + i,
                    InvoiceRef = invoiceId,
                    RowID = item.RowId,
                    ItemRef = item.ItemRef,
                    TracingRef = null,
                    StockRef = item.StockRef,
                    Quantity = item.Quantity,
                    SecondaryQuantity = null,
                    Fee = item.Fee,
                    Price = item.Price,
                    PriceInBaseCurrency = item.Price,
                    Discount = item.Discount,
                    DiscountInBaseCurrency = item.Discount,
                    PriceInfoDiscountRate = 0,
                    PriceInfoPriceDiscount = 0,
                    PriceInfoPercentDiscount = 0,
                    CustomerDiscount = 0,
                    CustomerDiscountRate = 0,
                    Addition = 0,
                    AdditionInBaseCurrency = 0,
                    Tax = item.Tax,
                    TaxInBaseCurrency = item.Tax,
                    Duty = 0,
                    DutyInBaseCurrency = 0,
                    NetPriceInBaseCurrency = item.NetPrice,
                    Rate = quotation.Rate,
                    QuotationItemRef = item.QuotationItemId,
                    Description = null,
                    Description_En = null,
                    DiscountInvoiceItemRef = null,
                    ProductPackRef = null,
                    ProductPackQuantity = null,
                    AggregateAmountDiscountRate = 0,
                    AggregateAmountPriceDiscount = 0,
                    AggregateAmountPercentDiscount = 0,
                    OrderItemRef = null,
                    AdditionFactor_VatEffective = 0,
                    AdditionFactorInBaseCurrency_VatEffective = 0,
                    AdditionFactor_VatIneffective = 0,
                    AdditionFactorInBaseCurrency_VatIneffective = 0,
                    DiscountItemGroupRef = null,
                    BankFeeForCurrencySale = 0,
                    BankFeeForCurrencySaleInBaseCurrency = 0,
                    IsAggregateDiscountInvoiceItem = false,
                    TaxPayerCurrencyPurchaseRate = 0
                });
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return invoiceId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }


}

public sealed class Sepidar1InvoiceExportRepository(Sepidar1ContextDb db) : SepidarInvoiceExportRepository(db) { }
public sealed class Sepidar2InvoiceExportRepository(Sepidar2ContextDb db) : SepidarInvoiceExportRepository(db) { }
