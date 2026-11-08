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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
            throw new InvalidOperationException($"فاکتور به پیش‌فاکتور انتخاب‌شده تعلق ندارد (پیش‌فاکتور فاکتور: {factor.PfactorID}، پیش‌فاکتور انتخاب‌شده: {preFactor.Id}).");

        if (factor.ParentReturnedId.HasValue || factor.Returned)
            throw new InvalidOperationException($"صدور فاکتور برگشتی پشتیبانی نمی‌شود (شماره فاکتور: {factor.SerialNumber}).");

        if (factor.SerialNumber <= 0 || !factor.DateMiladi.HasValue)
            throw new InvalidOperationException($"شماره یا تاریخ فاکتور CRM نامعتبر است (شماره: {factor.SerialNumber}، تاریخ: {factor.DateMiladi?.ToString("yyyy/MM/dd") ?? "ثبت نشده"}).");

        var fiscalYear = await context.FiscalYears.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FiscalYearId == quotation.FiscalYearRef, ct);

        if (fiscalYear == null || factor.DateMiladi.Value.Date < fiscalYear.StartDate.Date
            || factor.DateMiladi.Value.Date > fiscalYear.EndDate.Date)
            throw new InvalidOperationException($"تاریخ فاکتور خارج از سال مالی پیش‌فاکتور سپیدار است (تاریخ فاکتور: {factor.DateMiladi:yyyy/MM/dd}، بازه سال مالی: {(fiscalYear == null ? "یافت نشد" : $"{fiscalYear.StartDate:yyyy/MM/dd} تا {fiscalYear.EndDate:yyyy/MM/dd}")}).");

        if (quotation.CustomerPartyRef <= 0 || quotation.SaleTypeRef <= 0)
            throw new InvalidOperationException($"مشتری یا نوع فروش پیش‌فاکتور سپیدار نامعتبر است (مشتری: {quotation.CustomerPartyRef}، نوع فروش: {quotation.SaleTypeRef}).");

        var preFactorItems = (preFactor.PreFactorItems ?? Array.Empty<PreFactorItem>())
            .Where(x => !x.IsDeleted).ToList();
        var factorItems = (factor.FactorItems ?? Array.Empty<FactorItem>())
            .Where(x => !x.IsDeleted).OrderBy(x => x.Row).ToList();

        if (preFactorItems.Count == 0 || factorItems.Count == 0)
            throw new InvalidOperationException($"پیش‌فاکتور یا فاکتور ردیف معتبر ندارد (ردیف‌های پیش‌فاکتور: {preFactorItems.Count}، ردیف‌های فاکتور: {factorItems.Count}).");

        if (factorItems.Any(x => !x.PfItemId.HasValue)
            || factorItems.Select(x => x.PfItemId).Distinct().Count() != factorItems.Count)
            throw new InvalidOperationException($"ردیف‌های فاکتور به ردیف‌های یکتای پیش‌فاکتور CRM متصل نیستند (شماره فاکتور: {factor.SerialNumber}).");

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
                    $"ارتباط ردیف فاکتور با کالای پیش‌فاکتور سپیدار مشخص نیست (ردیف: {factorItem.Row}، ردیف پیش‌فاکتور CRM: {factorItem.PfItemId}).");

            if (quotationItem.ItemRef <= 0 || quotationItem.StockRef <= 0)
                throw new InvalidOperationException($"کالا یا انبار ردیف پیش‌فاکتور سپیدار نامعتبر است (ردیف: {factorItem.Row}، کالا: {quotationItem.ItemRef}، انبار: {quotationItem.StockRef}).");

            // The older quotation exporter assigned RowID by collection order.
            // Verify the original quantity and fee before trusting the row correspondence.
            if (!decimal.TryParse(sourceItem.WeightCount, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var sourceQuantity)
                || sourceQuantity != quotationItem.Quantity
                || sourceItem.UnitAmount != quotationItem.Fee)
                throw new InvalidOperationException(
                    $"تعداد یا قیمت ردیف CRM با پیش‌فاکتور سپیدار متفاوت است (ردیف: {factorItem.Row}، تعداد CRM: {sourceItem.WeightCount}، سپیدار: {quotationItem.Quantity:N3}، قیمت CRM: {sourceItem.UnitAmount:N0}، سپیدار: {quotationItem.Fee:N0}).");

            if (!decimal.TryParse(factorItem.WeightCount, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                throw new InvalidOperationException($"مقدار ردیف فاکتور نامعتبر است (ردیف: {factorItem.Row}، مقدار: {factorItem.WeightCount}).");

            // Source quotation can contain more quantity than this invoice (partial invoicing).
            if (quantity > quotationItem.Quantity)
                throw new InvalidOperationException($"مقدار فاکتور از پیش‌فاکتور سپیدار بیشتر است (ردیف: {factorItem.Row}، فاکتور: {quantity:N3}، پیش‌فاکتور: {quotationItem.Quantity:N3}).");

            var price = Math.Round(quantity * factorItem.UnitAmount);
            var discount = factorItem.Discount;
            var tax = factorItem.TaxAmount;
            if (price < 0 || discount < 0 || discount > price || tax < 0
                || factorItem.TotalAmount != price - discount + tax)
                throw new InvalidOperationException($"مبلغ ردیف فاکتور با محاسبات برابر نیست (ردیف: {factorItem.Row}، مبلغ ثبت‌شده: {factorItem.TotalAmount:N0}، مبلغ محاسبه‌شده: {price - discount + tax:N0}).");

            lines.Add(new SepidarInvoiceExportItemPreview {
                RowId = lines.Count + 1,
                QuotationItemId = quotationItem.QuotationItemID,
                ItemRef = quotationItem.ItemRef,
                ItemTitle = factorItem.ProductSmallName ?? factorItem.Product,
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
            throw new InvalidOperationException($"جمع ردیف‌های فاکتور با جمع کل CRM برابر نیست (مبلغ ردیف‌ها: {lines.Sum(x => x.NetPrice):N0}، مبلغ فاکتور: {factor.PaymentAmount:N0}، تخفیف ردیف‌ها: {lines.Sum(x => x.Discount):N0}، تخفیف فاکتور: {factor.Discount:N0}، مالیات ردیف‌ها: {lines.Sum(x => x.Tax):N0}، مالیات فاکتور: {factor.TotalTax:N0}).");

        var lineIds = lines.Select(x => x.QuotationItemId).ToArray();
        var previousQuantities = await context.SalesInvoiceItems.AsNoTracking()
            .Where(x => lineIds.Contains(x.QuotationItemRef))
            .Join(context.SalesInvoices.AsNoTracking().Where(x => x.State == 1
                    && !(x.Number == factor.SerialNumber && x.QuotationRef == quotationId)),
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
                    $"مجموع مقدار فاکتورهای صادرشده از پیش‌فاکتور سپیدار بیشتر است (ردیف: {line.RowId}، کالا: {line.ItemTitle}، پیش‌فاکتور: {quotedQuantity:N3}، قبلی: {previousQuantity:N3}، فعلی: {line.Quantity:N3}، مازاد: {line.Quantity + previousQuantity - quotedQuantity:N3}).");
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
            InvoiceNumber = factor.SerialNumber,
            InvoiceDate = factor.DateMiladi.Value,
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
        var existing = await context.SalesInvoices.AsNoTracking()
            .Where(x => x.Number == factor.SerialNumber && x.FiscalYearRef == preview.FiscalYearRef)
            .Select(x => new { x.InvoiceId, x.QuotationRef }).ToListAsync(ct);

        if (existing.Count != 0)
        {
            if (existing.Count == 1 && existing[0].QuotationRef == quotationId)
            {
                var priorInvoice = await context.SalesInvoices.AsNoTracking()
                    .SingleAsync(x => x.InvoiceId == existing[0].InvoiceId, ct);
                var priorItems = await context.SalesInvoiceItems.AsNoTracking()
                    .Where(x => x.InvoiceRef == priorInvoice.InvoiceId).ToListAsync(ct);

                var isSameInvoice = priorInvoice.Price == preview.Price
                    && priorInvoice.Discount == preview.Discount
                    && priorInvoice.Tax == preview.Tax
                    && priorInvoice.NetPriceInBaseCurrency == preview.NetPrice
                    && priorItems.Count == preview.Items.Count
                    && preview.Items.All(line => priorItems.Any(item =>
                        item.QuotationItemRef == line.QuotationItemId
                        && item.Quantity == line.Quantity
                        && item.Fee == line.Fee
                        && item.Price == line.Price
                        && item.Discount == line.Discount
                        && item.Tax == line.Tax));

                if (isSameInvoice)
                    return priorInvoice.InvoiceId;
            }

            throw new InvalidOperationException("شماره فاکتور در این سال مالی قبلاً برای سند متفاوتی استفاده شده است.");
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

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            // Check again inside the transaction to reduce duplicate submissions.
            if (await context.SalesInvoices.AsNoTracking()
                    .AnyAsync(x => x.Number == factor.SerialNumber
                        && x.FiscalYearRef == preview.FiscalYearRef, ct))
                throw new InvalidOperationException("در فاصله پیش‌نمایش تا ثبت، شماره فاکتور در سپیدار استفاده شده است.");

            var invoiceId = await NextIdAsync("SLS.Invoice", 1, ct);
            var lastItemId = await NextIdAsync("SLS.InvoiceItem", preview.Items.Count, ct);
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
                Number = factor.SerialNumber,
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
                Guid = null,
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

//            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return invoiceId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> NextIdAsync(string table, int count, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
DECLARE @Id int;
EXEC FMK.spGetNextId @TableName = @TableName, @Id = @Id OUTPUT, @IncValue = @IncValue;
SELECT @Id;";
        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@TableName";
        tableParameter.Value = table;
        command.Parameters.Add(tableParameter);

        var countParameter = command.CreateParameter();
        countParameter.ParameterName = "@IncValue";
        countParameter.Value = count;
        command.Parameters.Add(countParameter);

        var currentTransaction = context.Database.CurrentTransaction;
        if (currentTransaction != null)
            command.Transaction = currentTransaction.GetDbTransaction();

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }
}

public sealed class Sepidar1InvoiceExportRepository(Sepidar1ContextDb db) : SepidarInvoiceExportRepository(db) { }
public sealed class Sepidar2InvoiceExportRepository(Sepidar2ContextDb db) : SepidarInvoiceExportRepository(db) { }
