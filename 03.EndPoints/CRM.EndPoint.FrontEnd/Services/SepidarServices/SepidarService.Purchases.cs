using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Framework;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    public sealed record PurchaseOption(int Id, string Code, string Title);
    public sealed record PurchaseItemSelection(Guid SupplyFactorItemId, int ItemId, string Description);
    public sealed record PurchaseDraft(
        InventoryPurchaseInvoice Invoice, IReadOnlyList<InventoryPurchaseInvoiceItem> Items);
    public sealed record PurchaseVoucherLine(int AccountId, int? DlId, decimal Debit, decimal Credit);
    public sealed record PurchaseDocument(int Id, int Number, DateTime Date, int? VoucherId,
        string? PartyTitle = null, int? BasePurchaseId = null, DateTime? CreationDate = null);
    public sealed record PurchaseReceiptDraft(
        InventoryReceipt Receipt, IReadOnlyList<InventoryReceiptItem> Items,
        IReadOnlyList<PurchaseVoucherLine> VoucherLines, int? SupplierInvoiceNumber, string? DelivererTitle);

    public async Task<PurchaseDraft> PreparePurchaseDraftAsync(
        SupplyFactor source, int vendorId, IReadOnlyList<PurchaseItemSelection> selections,
        string description, CancellationToken ct)
    {
        if (source.Items == null || source.Items.Count == 0 || selections == null
            || selections.Count != source.Items.Count || selections.Any(x => x is null)
            || selections.Select(x => x.SupplyFactorItemId).Distinct().Count() != selections.Count)
            throw new InvalidOperationException("برای هر ردیف فاکتور تأمین یک کالای متناظر انتخاب کنید.");
        if (description?.Length > 400 || selections.Any(x => x.Description?.Length > 500))
            throw new InvalidOperationException("توضیحات فاکتور خرید یا یکی از ردیف‌ها بیش از حد مجاز است.");

        var vendorDlId = await sepidarContext.Parties.AsNoTracking()
            .Where(x => x.PartyId == vendorId && x.IsVendor && x.Dl != null)
            .Select(x => (int?)x.DLRef)
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("تأمین‌کننده معتبر در سپیدار پیدا نشد.");
        var selectedItems = selections.ToDictionary(x => x.SupplyFactorItemId);
        var itemIds = selections.Select(x => x.ItemId).Distinct().ToArray();
        var items = await sepidarContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemID) && x.IsActive && x.Type == 1
                && !x.SecondaryUnitRef.HasValue && !x.SerialTracking && !x.IsPricingBasedOnTracing)
            .Select(x => new { x.ItemID, x.UnitRef })
            .ToDictionaryAsync(x => x.ItemID, ct);
        var unitIds = items.Values.Select(x => x.UnitRef).Distinct().ToArray();
        var units = await sepidarContext.Units.AsNoTracking().Where(x => unitIds.Contains(x.UnitID))
            .ToDictionaryAsync(x => x.UnitID, x => x.Title, ct);

        var rows = new List<InventoryPurchaseInvoiceItem>(source.Items.Count);
        foreach (var line in source.Items)
        {
            if (!selectedItems.TryGetValue(line.Id, out var selected)
                || !items.TryGetValue(selected.ItemId, out var item)
                || !units.TryGetValue(item.UnitRef, out var unit))
                throw new InvalidOperationException("کالای متناظر یک یا چند ردیف در سپیدار معتبر نیست.");
            if (!SupplyPurchaseUnit.Matches(line.UnitTxt, unit))
                throw new InvalidOperationException($"واحد کالای ردیف {rows.Count + 1} با واحد اصلی سپیدار یکسان نیست.");

            if (line.WeightCount <= 0 || line.UnitAmount < 0 || line.Discount < 0 || line.TaxAmount < 0
                || !FitsPurchaseDecimal(line.WeightCount) || !FitsPurchaseDecimal(line.UnitAmount)
                || !FitsPurchaseDecimal(line.Discount) || !FitsPurchaseDecimal(line.TaxAmount))
                throw new InvalidOperationException($"مقدار یا مبلغ ردیف {rows.Count + 1} معتبر نیست.");

            var price = decimal.Round(line.WeightCount * line.UnitAmount, 0, MidpointRounding.AwayFromZero);
            var net = price - line.Discount + line.TaxAmount;
            if (!FitsPurchaseDecimal(price) || !FitsPurchaseDecimal(net)
                || line.Discount > price || net != line.TotalAmount)
                throw new InvalidOperationException($"جمع ردیف {rows.Count + 1} با مبلغ فاکتور تأمین تطبیق ندارد.");

            rows.Add(new InventoryPurchaseInvoiceItem {
                RowNumber = rows.Count + 1,
                ItemRef = selected.ItemId,
                Quantity = line.WeightCount,
                RemainingQuantity = line.WeightCount,
                CurrencyRate = 1m,
                Fee = line.UnitAmount,
                FeeInBaseCurrency = line.UnitAmount,
                Price = price,
                PriceInBaseCurrency = price,
                Discount = line.Discount,
                DiscountInBaseCurrentcy = line.Discount,
                Tax = line.TaxAmount,
                TaxInBaseCurrency = line.TaxAmount,
                Duty = 0m, // فعلا
                DutyInBaseCurrency = 0m,
                NetPriceInBaseCurrency = net,
                Description = selected.Description,
                Version = 1
            });
        }

        var gross = rows.Sum(x => x.Price);
        var discount = rows.Sum(x => x.Discount.GetValueOrDefault());
        var tax = rows.Sum(x => x.Tax.GetValueOrDefault());
        var total = gross - discount + tax;
        if (gross != source.SumTotalAmount || discount != source.Discount || tax != source.TotalTax
            || total != source.PaymentAmount || !FitsPurchaseDecimal(gross)
            || !FitsPurchaseDecimal(tax) || !FitsPurchaseDecimal(total))
            throw new InvalidOperationException("جمع اقلام با مبلغ فاکتور تأمین تطبیق ندارد.");

        var date = source.DateMiladi.Date;
        var serial = TextService.ToEnglishNumber(source.SerialNumber ?? "").Trim();
        int? supplierNumber = int.TryParse(serial, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number > 0 ? number : null;
        var currencyId = await sepidarContext.Currencies.AsNoTracking()
            .Where(x => x.Title.Trim() == "ریال" || x.Title.Trim() == "ريال")
            .Select(x => (int?)x.CurrencyID)
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("ارز ریال در سپیدار پیدا نشد.");
        var invoice = new InventoryPurchaseInvoice {
            VendorDLRef = vendorDlId,
            InvoiceNumber = supplierNumber,
            Date = date,
            CurrencyRef = currencyId,
            CurrencyRate = 1m,
            TotalPrice = gross,
            TotalPriceInBaseCurrency = gross,
            TotalTax = tax,
            TotalDuty = 0m, // فعلا
            TotalTransportPrice = 0m,
            TotalNetPrice = total,
            TotalNetPriceInBaseCurrency = total,
            FiscalYearRef = await GetFiscalYearIdAsync(date, ct),
            Type = 1,
            Version = 1,
            Description = description
        };
        return new PurchaseDraft(invoice, rows);
    }

    private static bool FitsPurchaseDecimal(decimal value) =>
        Math.Abs(value) < 1_000_000_000_000_000m && decimal.Round(value, 4) == value;

    public async Task<IReadOnlyList<PurchaseOption>> GetPurchaseVendorsAsync(int? selectedId, CancellationToken ct)
    {
        var query = sepidarContext.Parties.AsNoTracking().Where(x => x.IsVendor && x.Dl != null);
        if (selectedId is int id)
            query = query.Where(x => x.PartyId == id);

        var options = await query.OrderBy(x => x.Dl.Code).ThenBy(x => x.PartyId)
            .Select(x => new PurchaseOption(x.PartyId, x.Dl.Code, x.Name + " " + x.LastName))
            .ToListAsync(ct);
        return options.ConvertAll(x => x with { Title = TextService.NormalizePersianText(x.Title).Trim() });
    }

    public async Task<IReadOnlyList<PurchaseOption>> GetPurchaseItemsAsync(CancellationToken ct)
    {
        return await sepidarContext.Items.AsNoTracking()
            .Where(x => x.IsActive && x.Type == 1 && !x.SecondaryUnitRef.HasValue
                && !x.SerialTracking && !x.IsPricingBasedOnTracing)
            .OrderBy(x => x.Code).ThenBy(x => x.ItemID)
            .Select(x => new PurchaseOption(x.ItemID, x.Code, x.Title))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PurchaseOption>> GetPurchaseReceiptStocksAsync(
        int purchaseId, CancellationToken ct)
    {
        var itemIds = await sepidarContext.InventoryPurchaseInvoiceItems.AsNoTracking()
            .Where(x => x.InventoryPurchaseInvoiceRef == purchaseId)
            .Select(x => x.ItemRef).Distinct().ToArrayAsync(ct);
        return await GetReceiptStocksForItemsAsync(itemIds, ct);
    }

    internal async Task<IReadOnlyList<PurchaseOption>> GetReceiptStocksForItemsAsync(int[] itemIds, CancellationToken ct)
    {
        if (itemIds.Length == 0)
            return [];

        var stocks = await sepidarContext.ItemStocks.AsNoTracking()
            .Where(x => x.Stock.IsActive && itemIds.Contains(x.ItemRef))
            .GroupBy(x => new { x.StockRef, x.Stock.Code, x.Stock.Title })
            .Where(group => group.Select(x => x.ItemRef).Distinct().Count() == itemIds.Length)
            .OrderBy(group => group.Key.Code)
            .Select(group => new { group.Key.StockRef, group.Key.Code, group.Key.Title })
            .ToListAsync(ct);
        return stocks.ConvertAll(x => new PurchaseOption(x.StockRef, x.Code.ToString(), x.Title));
    }

    public sealed record PurchaseReceiptSource(
        InventoryPurchaseInvoice Invoice, IReadOnlyList<InventoryPurchaseInvoiceItem> Items);

    public async Task<PurchaseReceiptSource> GetFullPurchaseReceiptSourceAsync(
        int purchaseId, CancellationToken ct)
    {
        var invoice = await sepidarContext.InventoryPurchaseInvoices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InventoryPurchaseInvoiceID == purchaseId && x.Type == 1, ct)
            ?? throw new InvalidOperationException("فاکتور خرید عادی در سپیدار پیدا نشد.");

        if (await sepidarContext.InventoryReceipts.AsNoTracking()
            .AnyAsync(x => x.BasePurchaseInvoiceRef == purchaseId && !x.IsReturn, ct))
            throw new InvalidOperationException("برای این فاکتور خرید قبلاً رسید انبار ثبت شده است.");

        var items = await sepidarContext.InventoryPurchaseInvoiceItems.AsNoTracking()
            .Where(x => x.InventoryPurchaseInvoiceRef == purchaseId)
            .OrderBy(x => x.RowNumber).ThenBy(x => x.InventoryPurchaseInvoiceItemID)
            .ToListAsync(ct);
        if (items.Count == 0 || items.Any(x => x.Quantity <= 0 || x.RemainingQuantity != x.Quantity
            || x.RemainingSecondaryQuantity != x.SecondaryQuantity))
            throw new InvalidOperationException("مانده اقلام خرید برای ثبت یک رسید کامل معتبر نیست.");

        return new PurchaseReceiptSource(invoice, items);
    }

    public async Task<PurchaseReceiptDraft> PreparePurchaseReceiptDraftAsync(int purchaseId,
        int stockId, DateTime date, string description, IReadOnlyDictionary<int, string> rowDescriptions,
        CancellationToken ct)
    {
        var source = await GetFullPurchaseReceiptSourceAsync(purchaseId, ct);
        date = date.Date;
        if (date < source.Invoice.Date.Date)
            throw new InvalidOperationException("تاریخ رسید نمی‌تواند پیش از تاریخ فاکتور خرید باشد.");

        var sourceRowIds = source.Items.Select(x => x.InventoryPurchaseInvoiceItemID).ToHashSet();
        if (description?.Length > 4000 || (rowDescriptions != null
            && rowDescriptions.Any(x => !sourceRowIds.Contains(x.Key) || x.Value?.Length > 4000)))
            throw new InvalidOperationException("توضیحات رسید یا ردیف‌های آن معتبر نیست.");

        var eligibleStocks = await GetPurchaseReceiptStocksAsync(purchaseId, ct);
        if (!eligibleStocks.Any(x => x.Id == stockId))
            throw new InvalidOperationException("انبار انتخاب‌شده برای همه اقلام فاکتور خرید معتبر نیست.");

        var fiscalYearId = await GetFiscalYearIdAsync(date, ct);
        var payableAccountId = await GetConfiguredAccountIdAsync("InventoryPayableSL", ct);
        var transportBrokerAccountId = await GetConfiguredAccountIdAsync("InventoryTransportBrokerSL", ct);
        var issueVoucher = await GetRequiredSepidarBooleanSettingAsync("AutomaticVoucherForInventoryReceipt", ct);

        var rows = new List<InventoryReceiptItem>(source.Items.Count);
        foreach (var item in source.Items)
        {
            if (item.CurrencyRate != 1m || item.TransportPrice.GetValueOrDefault() != 0m
                || item.TransportTax.GetValueOrDefault() != 0m || item.TransportDuty.GetValueOrDefault() != 0m
                || item.Duty.GetValueOrDefault() != 0m || item.Addition.GetValueOrDefault() != 0m
                || item.InsuranceAmount.GetValueOrDefault() != 0m || item.WithHoldingTaxAmount.GetValueOrDefault() != 0m)
                throw new InvalidOperationException("این فاکتور خرید دارای مبالغ تکمیلی خارج از گردش رسید فعلی است.");

            var price = item.Price - item.Discount.GetValueOrDefault();
            var tax = item.Tax.GetValueOrDefault();
            if (price < 0m || price + tax != item.NetPrice)
                throw new InvalidOperationException("مبلغ ردیف رسید با ردیف فاکتور خرید تطبیق ندارد.");
            rows.Add(new InventoryReceiptItem {
                RowNumber = rows.Count + 1,
                Base = source.Invoice.Number,
                BasePurchaseInvoiceItemRef = item.InventoryPurchaseInvoiceItemID,
                ItemRef = item.ItemRef,
                TracingRef = item.TracingRef,
                Quantity = item.Quantity,
                SecondaryQuantity = item.SecondaryQuantity,
                Price = price,
                Tax = tax,
                Duty = 0m, // فعلا
                Description = rowDescriptions?.GetValueOrDefault(item.InventoryPurchaseInvoiceItemID),
                Version = 1
            });
        }

        var totalPrice = rows.Sum(x => x.Price.GetValueOrDefault());
        var totalTax = rows.Sum(x => x.Tax.GetValueOrDefault());
        var total = totalPrice + totalTax;
        if (total != source.Invoice.TotalNetPrice || decimal.Round(totalPrice, 3) != totalPrice
            || decimal.Round(totalTax, 3) != totalTax || !FitsPurchaseDecimal(total))
            throw new InvalidOperationException("جمع رسید انبار با مبلغ فاکتور خرید تطبیق ندارد.");

        var receipt = new InventoryReceipt {
            Type = 1,
            PurchaseType = 1,
            StockRef = stockId,
            DelivererDLRef = source.Invoice.VendorDLRef,
            SLAccountRef = payableAccountId,
            TransportBrokerSLAccountRef = transportBrokerAccountId,
            TransporterDLRef = source.Invoice.VendorDLRef,
            Date = date,
            TotalPrice = totalPrice,
            TotalTax = totalTax,
            TotalDuty = 0m, // فعلا
            TotalTransportPrice = 0m,
            TotalNetPrice = total,
            TotalOtherCost = 0m,
            FiscalYearRef = fiscalYearId,
            BasePurchaseInvoiceRef = purchaseId,
            CreatorForm = 1,
            Version = 1,
            Description = description
        };
        var voucherLines = issueVoucher ? await PreparePurchaseReceiptVoucherLinesAsync(receipt, ct) : [];
        var delivererTitle = issueVoucher
            ? await sepidarContext.DLs.AsNoTracking().Where(x => x.DLId == source.Invoice.VendorDLRef)
                .Select(x => x.Title).SingleAsync(ct)
            : null;
        return new PurchaseReceiptDraft(receipt, rows, voucherLines, source.Invoice.InvoiceNumber, delivererTitle);
    }

    private async Task<IReadOnlyList<PurchaseVoucherLine>> PreparePurchaseReceiptVoucherLinesAsync(
        InventoryReceipt receipt, CancellationToken ct)
    {
        var stockAccountId = await sepidarContext.Stocks.AsNoTracking()
            .Where(x => x.StockID == receipt.StockRef && x.IsActive)
            .Select(x => x.AccountSLRef).SingleOrDefaultAsync(ct);
        if (!await sepidarContext.Accounts.AsNoTracking()
            .AnyAsync(x => x.AccountId == stockAccountId && x.IsActive && !x.HasDL, ct))
            throw new InvalidOperationException("حساب معین انبار برای صدور سند حسابداری معتبر نیست.");

        var payableAccountId = receipt.SLAccountRef!.Value;
        var payableDlId = await GetVoucherItemDlRefAsync(payableAccountId, receipt.DelivererDLRef!.Value, ct)
            ?? throw new InvalidOperationException("حساب پرداختنی انبار باید تفصیلی تأمین‌کننده داشته باشد.");
        var price = receipt.TotalPrice.GetValueOrDefault();
        var tax = receipt.TotalTax.GetValueOrDefault();
        var lines = new List<PurchaseVoucherLine> { new(stockAccountId, null, price, 0m) };

        if (tax > 0m)
        {
            var taxAccountId = await GetConfiguredAccountIdAsync("PrepaymentTaxSL", ct);
            var taxDlId = await GetConfiguredDlIdAsync("ValueAddedTaxDL", ct);
            var voucherTaxDlId = await GetVoucherItemDlRefAsync(taxAccountId, taxDlId, ct);
            lines.Add(new PurchaseVoucherLine(taxAccountId, voucherTaxDlId, tax, 0m));
        }

        lines.Add(new PurchaseVoucherLine(payableAccountId, payableDlId, 0m, price));
        if (tax > 0m)
            lines.Add(new PurchaseVoucherLine(payableAccountId, payableDlId, 0m, tax));

        return lines;
    }

    public async Task<PurchaseDocument> FindPurchaseDocumentAsync(bool receipt, int id, CancellationToken ct)
    {
        if (receipt)
            return await sepidarContext.InventoryReceipts.AsNoTracking()
                .Where(x => x.InventoryReceiptID == id && !x.IsReturn && x.BasePurchaseInvoiceRef != null)
                .Select(x => new PurchaseDocument(x.InventoryReceiptID, x.Number, x.Date, x.AccountingVoucherRef,
                    sepidarContext.DLs.Where(dl => dl.DLId == x.DelivererDLRef).Select(dl => dl.Title).FirstOrDefault(),
                    x.BasePurchaseInvoiceRef, x.CreationDate))
                .SingleOrDefaultAsync(ct);
        return await sepidarContext.InventoryPurchaseInvoices.AsNoTracking()
            .Where(x => x.InventoryPurchaseInvoiceID == id && x.Type == 1)
            .Select(x => new PurchaseDocument(x.InventoryPurchaseInvoiceID, x.Number, x.Date, x.AccountingVoucherRef,
                sepidarContext.DLs.Where(dl => dl.DLId == x.VendorDLRef).Select(dl => dl.Title).FirstOrDefault(),
                null, x.CreationDate))
            .SingleOrDefaultAsync(ct);
    }

    public Task<bool> PurchaseDocumentExistsAsync(bool receipt, int id, CancellationToken ct) =>
        receipt
            ? sepidarContext.InventoryReceipts.AnyAsync(x => x.InventoryReceiptID == id, ct)
            : sepidarContext.InventoryPurchaseInvoices.AnyAsync(x => x.InventoryPurchaseInvoiceID == id, ct);

    public Task<Dictionary<int, string>> GetPurchaseItemLabelsAsync(int[] ids, CancellationToken ct) =>
        sepidarContext.Items.AsNoTracking().Where(x => ids.Contains(x.ItemID))
            .ToDictionaryAsync(x => x.ItemID, x => x.Code + " - " + x.Title, ct);

    public sealed record ReceiptItemDisplay(string Label, string UnitTitle);

    public Task<Dictionary<int, ReceiptItemDisplay>> GetReceiptItemDisplaysAsync(int[] itemIds, CancellationToken ct) =>
        (from item in sepidarContext.Items.AsNoTracking()
         where itemIds.Contains(item.ItemID)
         join unit in sepidarContext.Units.AsNoTracking() on item.UnitRef equals unit.UnitID into units
         from unit in units.DefaultIfEmpty()
         select new { item.ItemID, Label = item.Code + " - " + item.Title, UnitTitle = unit.Title })
        .ToDictionaryAsync(x => x.ItemID, x => new ReceiptItemDisplay(x.Label, x.UnitTitle), ct);

    public Task<List<PurchaseDocument>> FindReceiptsForPurchaseAsync(int purchaseId, CancellationToken ct) =>
        QueryReceiptsForPurchase(purchaseId).ToListAsync(ct);

    public Task<PurchaseDocument> FindReceiptForPurchaseAsync(int purchaseId, CancellationToken ct) =>
        QueryReceiptsForPurchase(purchaseId).FirstOrDefaultAsync(ct);

    private IQueryable<PurchaseDocument> QueryReceiptsForPurchase(int purchaseId) =>
        sepidarContext.InventoryReceipts.AsNoTracking()
            .Where(x => x.BasePurchaseInvoiceRef == purchaseId && !x.IsReturn)
            .OrderBy(x => x.InventoryReceiptID)
            .Select(x => new PurchaseDocument(x.InventoryReceiptID, x.Number, x.Date, x.AccountingVoucherRef,
                sepidarContext.DLs.Where(dl => dl.DLId == x.DelivererDLRef).Select(dl => dl.Title).FirstOrDefault(),
                x.BasePurchaseInvoiceRef, x.CreationDate));

    public async Task<int> ReservePurchaseDocumentIdAsync(bool receipt, CancellationToken ct)
    {
        var table = receipt ? "INV.InventoryReceipt" : "INV.InventoryPurchaseInvoice";
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var id = await GetLockedNextPurchaseIdAsync(table, 1, ct);
        await transaction.CommitAsync(ct);
        return id;
    }

    private async Task<int> GetLockedNextPurchaseIdAsync(string table, int count, CancellationToken ct)
    {
        await sepidarContext.Database.SqlQuery<int>(
            $"SELECT LastId AS [Value] FROM FMK.IDGeneration WITH (XLOCK,HOLDLOCK) WHERE TableName = {table}")
            .ToListAsync(ct);
        return await GetNextIdAsync(table, count, ct);
    }

    public async Task<PurchaseDocument> AddSupplyPurchaseAsync(int id, int creator, SupplyFactor source,
        int vendorId, IReadOnlyList<PurchaseItemSelection> selections, string description,
        string expectedPreview, CancellationToken ct)
    {
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await ValidatePurchaseCreatorAsync(creator, ct);
        var draft = await PreparePurchaseDraftAsync(source, vendorId, selections, description, ct);
        ValidatePurchasePreview(draft, expectedPreview);
        var invoice = draft.Invoice;
        var number = await sepidarContext.Database.SqlQuery<int?>(
            $"SELECT MAX(Number) AS [Value] FROM INV.InventoryPurchaseInvoice WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={invoice.FiscalYearRef} AND Type=1")
            .SingleAsync(ct);
        invoice.Number = await ResolvePurchaseDocumentNumberAsync(false, number, ct);
        invoice.InventoryPurchaseInvoiceID = id;
        invoice.Creator = invoice.LastModifier = creator;
        invoice.CreationDate = invoice.LastModificationDate = DateTime.Now;
        var lastItemId = await GetLockedNextPurchaseIdAsync("INV.InventoryPurchaseInvoiceItem", draft.Items.Count, ct);
        foreach (var row in draft.Items)
        {
            row.InventoryPurchaseInvoiceItemID = lastItemId - draft.Items.Count + row.RowNumber;
            row.InventoryPurchaseInvoiceRef = id;
        }

        sepidarContext.InventoryPurchaseInvoices.Add(invoice);
        sepidarContext.InventoryPurchaseInvoiceItems.AddRange(draft.Items);
        await sepidarContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PurchaseDocument(id, invoice.Number, invoice.Date, null);
    }

    public async Task<PurchaseDocument> AddSupplyReceiptAsync(int id, int creator, int purchaseId, int stockId,
        DateTime date, string description, IReadOnlyDictionary<int, string> rowDescriptions,
        string expectedPreview, CancellationToken ct)
    {
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await ValidatePurchaseCreatorAsync(creator, ct);
        await sepidarContext.Database.SqlQuery<int>(
            $"SELECT InventoryPurchaseInvoiceID AS [Value] FROM INV.InventoryPurchaseInvoice WITH (UPDLOCK,HOLDLOCK) WHERE InventoryPurchaseInvoiceID={purchaseId}")
            .ToListAsync(ct);
        var draft = await PreparePurchaseReceiptDraftAsync(purchaseId, stockId, date, description, rowDescriptions, ct);
        ValidatePurchasePreview(draft, expectedPreview);
        var receipt = draft.Receipt;
        var number = await sepidarContext.Database.SqlQuery<int?>(
            $"SELECT MAX(Number) AS [Value] FROM INV.InventoryReceipt WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={receipt.FiscalYearRef} AND StockRef={stockId} AND IsReturn=0")
            .SingleAsync(ct);
        receipt.Number = await ResolvePurchaseDocumentNumberAsync(true, number, ct);
        receipt.InventoryReceiptID = id;
        receipt.Creator = receipt.LastModifier = creator;
        receipt.CreationDate = receipt.LastModificationDate = DateTime.Now;
        var lastItemId = await GetLockedNextPurchaseIdAsync("INV.InventoryReceiptItem", draft.Items.Count, ct);
        using var summary = CreateInventorySummaryTable(draft.Items.Select(row =>
            new InventoryStockKey(stockId, row.ItemRef, row.TracingRef)), receipt.FiscalYearRef);
        await LockInventoryStockSummaryAsync(summary, ct);
        await UpdateInventoryStockSummaryAsync(summary, ct);
        var keys = draft.Items.Select(x => new InventoryStockKey(stockId, x.ItemRef, x.TracingRef)).Distinct().ToArray();
        var before = await ReadInventoryStockBalancesAsync(keys, receipt.FiscalYearRef, ct);
        foreach (var row in draft.Items)
        {
            row.InventoryReceiptItemID = lastItemId - draft.Items.Count + row.RowNumber;
            row.InventoryReceiptRef = id;
        }

        if (draft.VoucherLines.Count > 0)
            receipt.AccountingVoucherRef = await AddPurchaseReceiptVoucherAsync(draft, ct);

        sepidarContext.InventoryReceipts.Add(receipt);
        sepidarContext.InventoryReceiptItems.AddRange(draft.Items);
        await sepidarContext.SaveChangesAsync(ct);
        var updated = await sepidarContext.InventoryPurchaseInvoiceItems
            .Where(x => x.InventoryPurchaseInvoiceRef == purchaseId)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.RemainingQuantity, 0m)
                .SetProperty(y => y.RemainingSecondaryQuantity, y => y.SecondaryQuantity == null ? null : (decimal?)0m), ct);
        if (updated != draft.Items.Count)
            throw new InvalidOperationException("مانده اقلام فاکتور خرید هم‌زمان تغییر کرده است.");

        // The first refresh removes empty summary rows. Recreate them before refreshing the receipt quantities.
        await LockInventoryStockSummaryAsync(summary, ct);
        await UpdateInventoryStockSummaryAsync(summary, ct);
        var after = await ReadInventoryStockBalancesAsync(keys, receipt.FiscalYearRef, ct);
        foreach (var group in draft.Items.GroupBy(x => new InventoryStockKey(stockId, x.ItemRef, x.TracingRef)))
        {
            var receivedQuantity = group.Sum(x => x.Quantity);
            var stockIncrease = after[group.Key].PhysicalQuantity - before[group.Key].PhysicalQuantity;
            if (stockIncrease != receivedQuantity)
            {
                var labels = await GetPurchaseItemLabelsAsync([group.Key.ItemRef], ct);
                var item = labels.GetValueOrDefault(group.Key.ItemRef) ?? group.Key.ItemRef.ToString(CultureInfo.InvariantCulture);
                throw new InvalidOperationException(
                    $"افزایش موجودی انبار برای «{item}» با مقدار رسید مطابقت ندارد. مقدار رسید {receivedQuantity:0.####} و تغییر موجودی {stockIncrease:0.####} است.");
            }
        }

        await transaction.CommitAsync(ct);
        return new PurchaseDocument(id, receipt.Number, receipt.Date, receipt.AccountingVoucherRef);
    }

    private async Task ValidatePurchaseCreatorAsync(int creator, CancellationToken ct)
    {
        if (!await sepidarContext.Database.SqlQuery<int>(
                $"SELECT UserID AS [Value] FROM FMK.[User] WHERE UserID={creator} AND Status=1 AND IsDeleted=0").AnyAsync(ct))
            throw new InvalidOperationException("کاربر ثبت خرید در نرم‌افزار مقصد معتبر یا فعال نیست.");
    }

    private sealed class PurchaseNumbering
    {
        public int Method { get; set; }
        public long StartValue { get; set; }
        public long? FinishValue { get; set; }
        public bool ApplyProperty1 { get; set; }
        public bool ApplyProperty2 { get; set; }
        public bool ApplyProperty3 { get; set; }
        public bool ApplyPorperty4 { get; set; }
    }

    private async Task<int> ResolvePurchaseDocumentNumberAsync(bool receipt, int? lastNumber, CancellationToken ct)
    {
        var entity = receipt
            ? "SG.Inventory.InventoryOperations.Common.InventoryReceiptRow"
            : "SG.Inventory.InventoryOperations.Common.InventoryPurchaseInvoiceRow";
        var setting = await sepidarContext.Database.SqlQuery<PurchaseNumbering>(
            $"SELECT Method,StartValue,FinishValue,ApplyProperty1,ApplyProperty2,ApplyProperty3,ApplyPorperty4 FROM FMK.NumberedEntity WHERE EntityFullName={entity}")
            .SingleOrDefaultAsync(ct);
        if (setting == null || setting.Method != 2 || !setting.ApplyProperty1
            || setting.ApplyProperty2 != receipt || setting.ApplyProperty3 || setting.ApplyPorperty4)
            throw new InvalidOperationException("روش شماره‌گذاری سند در سپیدار برای این گردش پشتیبانی نمی‌شود.");

        var next = Math.Max((long)lastNumber.GetValueOrDefault() + 1, setting.StartValue);
        if (next <= 0 || next > int.MaxValue || (setting.FinishValue.HasValue && next > setting.FinishValue))
            throw new InvalidOperationException("شماره بعدی سند از محدوده تنظیم‌شده در سپیدار خارج است.");
        return (int)next;
    }

    public static string GetPurchasePreviewToken<T>(T draft) => Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(draft)));

    private static void ValidatePurchasePreview<T>(T draft, string expectedPreview)
    {
        if (GetPurchasePreviewToken(draft) != expectedPreview)
            throw new InvalidOperationException("اطلاعات سند تغییر کرده است. پیش‌نمایش جدید را بررسی کنید.");
    }

    internal Task<bool> GetReceiptEditAutoVoucherAsync(CancellationToken ct) =>
        GetRequiredSepidarBooleanSettingAsync("AutomaticVoucherForInventoryReceipt", ct);

    internal async Task<int?> PostEditedReceiptAsync(SupplyReceiptPosting posting, bool expectedAutoVoucher, CancellationToken ct)
    {
        if (sepidarContext.Database.CurrentTransaction == null)
            throw new InvalidOperationException("تراکنش ویرایش رسید انبار آغاز نشده است.");
        var receipt = posting.Receipt;
        var autoVoucher = await GetReceiptEditAutoVoucherAsync(ct);
        if (autoVoucher != expectedAutoVoucher)
            throw new InvalidOperationException("تنظیم صدور خودکار سند حسابداری تغییر کرده است. فرم را دوباره باز کنید.");
        if (posting.StockChanged)
        {
            var lastNumber = await sepidarContext.Database.SqlQuery<int?>(
                $"SELECT MAX(Number) AS [Value] FROM INV.InventoryReceipt WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={receipt.FiscalYearRef} AND StockRef={receipt.StockRef} AND IsReturn=0")
                .SingleAsync(ct);
            receipt.Number = await ResolvePurchaseDocumentNumberAsync(true, lastNumber, ct);
        }
        if (!autoVoucher)
            return null;
        if (receipt.DelivererDLRef is not int deliverer || receipt.SLAccountRef is not > 0 ||
            receipt.TotalPrice is not >= 0 || receipt.TotalTax is not >= 0 ||
            receipt.TotalPrice + receipt.TotalTax != receipt.TotalNetPrice)
            throw new InvalidOperationException("مبالغ یا حساب‌های رسید برای صدور دوباره سند حسابداری معتبر نیست.");
        var lines = await PreparePurchaseReceiptVoucherLinesAsync(receipt, ct);
        var title = await sepidarContext.DLs.AsNoTracking().Where(x => x.DLId == deliverer)
            .Select(x => x.Title).SingleAsync(ct);
        return await AddPurchaseReceiptVoucherAsync(new(receipt, [], lines, posting.SupplierInvoiceNumber, title),
            ct, useModifierAsCreator: true);
    }

    private async Task<int> AddPurchaseReceiptVoucherAsync(PurchaseReceiptDraft draft, CancellationToken ct, bool useModifierAsCreator = false)
    {
        var receipt = draft.Receipt;
        var lines = draft.VoucherLines;
        var id = await GetLockedNextPurchaseIdAsync("ACC.Voucher", 1, ct);
        var voucherNumbers = await GetNextVoucherNumbersAsync(receipt.FiscalYearRef, ct);
        var description = $"بابت رسيد انبار شماره {receipt.Number} تاريخ {BaseServices.GetDatePersian(receipt.Date)}";
        var linePrefix = $"{description} نوع خريد (داخلي)";
        var invoiceSuffix = draft.SupplierInvoiceNumber.HasValue
            ? $" به شماره فاكتور خريد {draft.SupplierInvoiceNumber.Value}"
            : string.Empty;
        var creditDescription = linePrefix + invoiceSuffix;
        var debitDescription = creditDescription;
        if (!string.IsNullOrWhiteSpace(draft.DelivererTitle))
        {
            const string fromDeliverer = " از ";
            const int maxDescriptionLength = 250;
            var titleLength = maxDescriptionLength - creditDescription.Length - fromDeliverer.Length;
            var title = draft.DelivererTitle.Trim();
            debitDescription = linePrefix + fromDeliverer + title[..Math.Min(title.Length, titleLength)] + invoiceSuffix;
        }
        sepidarContext.Add(new Voucher {
            VoucherId = id,
            Number = voucherNumbers.Number,
            ReferenceNumber = voucherNumbers.ReferenceNumber,
            Date = receipt.Date,
            State = 1,
            Type = 2,
            FiscalYearRef = receipt.FiscalYearRef,
            Description = description,
            Description_En = description,
            Version = 1,
            Creator = useModifierAsCreator ? receipt.LastModifier : receipt.Creator,
            LastModifier = receipt.LastModifier,
            CreationDate = useModifierAsCreator ? receipt.LastModificationDate : receipt.CreationDate,
            LastModificationDate = receipt.LastModificationDate,
            DailyNumber = await GetNextVoucherDailyNumberAsync(receipt.Date, receipt.FiscalYearRef, ct),
            IssuerSystem = 1
        });
        var lastItemId = await GetLockedNextPurchaseIdAsync("ACC.VoucherItem", lines.Count, ct);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineDescription = line.Debit > 0m ? debitDescription : creditDescription;
            sepidarContext.Add(new VoucherItem {
                VoucherItemId = lastItemId - lines.Count + index + 1,
                VoucherRef = id,
                RowNumber = index + 1,
                AccountSLRef = line.AccountId,
                DLRef = line.DlId,
                Debit = line.Debit,
                Credit = line.Credit,
                IssuerEntityName = "SG.Inventory.InventoryOperations.Common.DsInventoryReceipt, SG.Inventory.InventoryOperations.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                IssuerEntityRef = receipt.InventoryReceiptID,
                Description = lineDescription,
                Description_En = lineDescription,
                Version = 1
            });
        }
        await sepidarContext.SaveChangesAsync(ct);
        return id;
    }
}
