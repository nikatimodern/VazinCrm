using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;

public sealed partial class SupplyDestinationMaintenanceRepository
{
    private const string RemovePartialReceipt = "ابتدا رسید انبار را از سوابق حذف کنید، سپس فاکتور خرید را همگام‌سازی کنید.";

    public async Task<SupplySyncStatus> GetSyncStatusAsync(SupplyDestinationDocument document, SupplyFactor source, CancellationToken ct)
    {
        var db = Resolve(document);
        var purchase = await db.InventoryPurchaseInvoices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InventoryPurchaseInvoiceID == document.PurchaseId, ct);
        if (purchase == null)
            return new(false, false);
        var purchaseRows = await db.InventoryPurchaseInvoiceItems.AsNoTracking()
            .Where(x => x.InventoryPurchaseInvoiceRef == document.PurchaseId).OrderBy(x => x.RowNumber)
            .ThenBy(x => x.InventoryPurchaseInvoiceItemID).ToListAsync(ct);
        var purchaseChanged = PurchaseChanged(source, purchase, purchaseRows);
        if (document.ReceiptId is not int receiptId || purchaseChanged)
            return new(purchaseChanged, false);
        var receipt = await db.InventoryReceipts.AsNoTracking().SingleOrDefaultAsync(x =>
            x.InventoryReceiptID == receiptId && x.BasePurchaseInvoiceRef == document.PurchaseId && !x.IsReturn, ct);
        if (receipt == null)
            return new(false, false);
        var receiptRows = await db.InventoryReceiptItems.AsNoTracking().Where(x => x.InventoryReceiptRef == receiptId).ToListAsync(ct);
        return new(false, ReceiptChanged(purchase, purchaseRows, receipt, receiptRows));
    }

    public async Task<SupplySyncPreview> GetSyncPreviewAsync(SupplyDestinationDocument document, SupplyFactor source,
        bool receiptOnly, CancellationToken ct) =>
        (await BuildSyncPlanAsync(Resolve(document), document, source, receiptOnly, false, ct)).Preview;

    public async Task SynchronizeAsync(SupplyDestinationDocument document, SupplyFactor source, bool receiptOnly, string token,
        bool autoVoucher, int modifier,
        Func<SupplySyncPreview, CancellationToken, Task<IReadOnlyList<InventoryPurchaseInvoiceItem>>> prepareRows,
        Func<bool, int, CancellationToken, Task<int>> allocateRowIds,
        Func<SupplyReceiptPosting, CancellationToken, Task<int?>> postVoucher, CancellationToken ct)
    {
        var db = Resolve(document);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await BuildSyncPlanAsync(db, document, source, receiptOnly, true, ct);
        if (plan.Preview.Blockers.Count > 0)
            throw new InvalidOperationException(plan.Preview.Blockers[0]);
        if (plan.Preview.Token != token)
            throw new InvalidOperationException("فاکتور تأمین یا اسناد سپیدار تغییر کرده‌اند. پیش‌نمایش را دوباره باز کنید.");

        var purchase = plan.Purchase.Purchase;
        var oldRows = plan.Purchase.PurchaseRows;
        var replacement = receiptOnly ? oldRows : await prepareRows(plan.Preview, ct);
        var receipt = plan.Receipt?.Receipt;
        var receiptRows = plan.Receipt?.ReceiptRows ?? [];
        var stockDelta = receipt is null ? null : await PrepareReceiptStockDeltaAsync(db, receipt, receiptRows, replacement, ct);
        var now = DateTime.Now;
        if (!receiptOnly)
        {
            var notes = MatchReceiptDescriptions(oldRows, receiptRows, replacement);
            if (receipt != null)
                await db.InventoryReceiptItems.Where(x => x.InventoryReceiptRef == receipt.InventoryReceiptID).ExecuteDeleteAsync(ct);
            await db.InventoryPurchaseInvoiceItems.Where(x => x.InventoryPurchaseInvoiceRef == document.PurchaseId).ExecuteDeleteAsync(ct);

            var lastPurchaseRow = await allocateRowIds(false, replacement.Count, ct);
            foreach (var row in replacement)
            {
                row.InventoryPurchaseInvoiceItemID = lastPurchaseRow - replacement.Count + row.RowNumber;
                row.InventoryPurchaseInvoiceRef = document.PurchaseId;
                row.RemainingQuantity = receipt == null ? row.Quantity : 0m;
            }
            db.InventoryPurchaseInvoiceItems.AddRange(replacement);
            db.Attach(purchase);
            purchase.TotalPrice = purchase.TotalPriceInBaseCurrency = source.SumTotalAmount;
            purchase.TotalTax = source.TotalTax;
            purchase.TotalNetPrice = purchase.TotalNetPriceInBaseCurrency = source.PaymentAmount;
            purchase.LastModificationDate = now;
            purchase.LastModifier = modifier;
            purchase.Version++;
            await db.SaveChangesAsync(ct);
            if (receipt != null)
            {
                var lastReceiptRow = await allocateRowIds(true, replacement.Count, ct);
                var newRows = replacement.Select(row => new InventoryReceiptItem {
                    InventoryReceiptItemID = lastReceiptRow - replacement.Count + row.RowNumber,
                    InventoryReceiptRef = receipt.InventoryReceiptID, RowNumber = row.RowNumber,
                    Base = purchase.Number, BasePurchaseInvoiceItemRef = row.InventoryPurchaseInvoiceItemID,
                    ItemRef = row.ItemRef, Quantity = row.Quantity, Price = row.Price - row.Discount.GetValueOrDefault(),
                    Tax = row.Tax.GetValueOrDefault(), Duty = 0m, Version = 1,
                    Description = notes.GetValueOrDefault(row.RowNumber)
                }).ToArray();
                db.InventoryReceiptItems.AddRange(newRows);
            }
        }
        else
        {
            var byId = oldRows.ToDictionary(x => x.InventoryPurchaseInvoiceItemID);
            foreach (var row in receiptRows)
            {
                var parent = byId[row.BasePurchaseInvoiceItemRef!.Value];
                db.Attach(row);
                row.Price = parent.Price - parent.Discount.GetValueOrDefault();
                row.Tax = parent.Tax.GetValueOrDefault();
                row.Version++;
            }
        }
        if (receipt != null)
        {
            db.Attach(receipt);
            SetReceiptTotals(receipt, replacement, purchase.TotalNetPrice);
            receipt.LastModificationDate = now;
            receipt.LastModifier = modifier;
            receipt.Version++;
            receipt.AccountingVoucherRef = await postVoucher(new(receipt, purchase.InvoiceNumber, false), ct);
            if (autoVoucher != receipt.AccountingVoucherRef.HasValue)
                throw new InvalidOperationException("صدور سند حسابداری رسید انبار تکمیل نشد.");
            await db.SaveChangesAsync(ct);
            if (plan.Receipt!.Voucher is { } voucher)
            {
                await DeleteSupplementaryDataAsync(db, VoucherType, voucher.VoucherId, ct);
                await db.Vouchers.Where(x => x.VoucherId == voucher.VoucherId).ExecuteDeleteAsync(ct);
            }
            await VerifyReceiptStockDeltaAsync(db, receipt, stockDelta!, ct);
        }
        await transaction.CommitAsync(ct);
    }

    private static async Task<SupplySyncPlan> BuildSyncPlanAsync(SepidarContextDbBase db, SupplyDestinationDocument document,
        SupplyFactor source, bool receiptOnly, bool forUpdate, CancellationToken ct)
    {
        var purchase = await BuildPlanAsync(db, document with { ReceiptId = null }, forUpdate, ct);
        var blockers = purchase.Blockers.ToList();
        var invoice = purchase.Purchase;
        var oldRows = purchase.PurchaseRows;
        var changed = PurchaseChanged(source, invoice, oldRows);
        if (receiptOnly && changed)
            blockers.Add("ابتدا فاکتور خرید را همگام‌سازی کنید.");
        else if (!receiptOnly && !changed)
            blockers.Add("فاکتور خرید با فاکتور تأمین همگام است.");
        if (source.Items.Count == 0)
            blockers.Add("فاکتور تأمین قلم فعالی ندارد.");
        var isRial = await db.Currencies.AsNoTracking().AnyAsync(x => x.CurrencyID == invoice.CurrencyRef &&
            (x.Title.Trim() == "ریال" || x.Title.Trim() == "ريال"), ct);
        if (!isRial || invoice.CurrencyRate != 1m || invoice.TotalDuty.GetValueOrDefault() != 0 || invoice.TotalTransportPrice.GetValueOrDefault() != 0 ||
            invoice.TotalInsuranceAmount.GetValueOrDefault() != 0 || invoice.TotalWithHoldingTaxAmount.GetValueOrDefault() != 0 ||
            oldRows.Any(UnsupportedPurchaseRow))
            blockers.Add("فاکتور خرید مبالغ یا اقلامی خارج از گردش همگام‌سازی دارد.");

        MaintenancePlan? receipt = null;
        if (purchase.HasLinkedReceipt || document.ReceiptId.HasValue || receiptOnly)
        {
            var linked = await db.InventoryReceipts.AsNoTracking().Where(x => x.BasePurchaseInvoiceRef == document.PurchaseId)
                .Select(x => new { x.InventoryReceiptID, x.IsReturn }).Take(2).ToListAsync(ct);
            if (linked.Count > 1)
                blockers.Insert(0, "ابتدا رسیدهای انبار وابسته را از سوابق حذف کنید، سپس فاکتور خرید را همگام‌سازی کنید.");
            else if (linked.Count != 1 || linked[0].IsReturn)
                blockers.Add("این فاکتور بیش از یک رسید، رسید برگشتی یا ارتباط انبار نامعتبر دارد. رسیدهای وابسته را در سپیدار بررسی کنید.");
            else if (linked[0].InventoryReceiptID != document.ReceiptId)
            {
                var linkedId = linked[0].InventoryReceiptID;
                var linkedRows = await db.InventoryReceiptItems.AsNoTracking().Where(x => x.InventoryReceiptRef == linkedId).ToListAsync(ct);
                blockers.Insert(0, IsFullReceipt(oldRows, linkedRows)
                    ? "این رسید مستقیماً در سپیدار ثبت شده یا سابقهٔ ERP آن معتبر نیست. رسید وابسته را بررسی کنید."
                    : RemovePartialReceipt);
            }
            else
            {
                receipt = await BuildPlanAsync(db, document, forUpdate, ct);
                var header = receipt.Receipt!;
                if (!IsFullReceipt(oldRows, receipt.ReceiptRows))
                    blockers.Insert(0, RemovePartialReceipt);
                blockers.AddRange(receipt.Blockers.Where(message => !blockers.Contains(message)).ToArray());
                if (header.Date.Date < invoice.Date.Date)
                    blockers.Add("تاریخ رسید انبار پیش از تاریخ فاکتور خرید است.");
                if (header.DelivererDLRef != invoice.VendorDLRef || header.TotalDuty.GetValueOrDefault() != 0 ||
                    header.TotalTransportPrice.GetValueOrDefault() != 0 || header.TotalOtherCost.GetValueOrDefault() != 0 ||
                    header.TotalReturnedPrice.GetValueOrDefault() != 0 || header.TotalReturnedNetPrice.GetValueOrDefault() != 0 ||
                    receipt.ReceiptRows.Any(UnsupportedReceiptRow))
                    blockers.Add("رسید انبار دارای اطلاعات تکمیلی یا گردش انبار خارج از این عملیات است.");
                var rowIds = oldRows.Select(x => x.InventoryPurchaseInvoiceItemID).ToArray();
                if (await db.InventoryReceiptItems.AnyAsync(x =>
                    (x.InventoryReceiptRef != header.InventoryReceiptID && x.BasePurchaseInvoiceItemRef.HasValue &&
                        rowIds.Contains(x.BasePurchaseInvoiceItemRef.Value)) ||
                    (x.ServiceInventoryPurchaseInvoiceRef.HasValue && rowIds.Contains(x.ServiceInventoryPurchaseInvoiceRef.Value)), ct))
                    blockers.Add("اقلام فاکتور خرید در رسید انبار دیگری استفاده شده‌اند.");
                if (receiptOnly && !ReceiptChanged(invoice, oldRows, header, receipt.ReceiptRows))
                    blockers.Add("رسید انبار با فاکتور خرید همگام است.");
            }
        }
        else if (oldRows.Any(row => row.Quantity != row.RemainingQuantity))
            blockers.Add(RemovePartialReceipt);
        if (!receiptOnly && blockers.Count == 0 &&
            await HasSupplySyncRowReferencesAsync(db, document.PurchaseId, receipt?.Receipt?.InventoryReceiptID, ct))
            blockers.Add("یکی از اقلام در سند دیگری در سپیدار استفاده شده است.");

        var itemIds = oldRows.Select(x => x.ItemRef).Concat(receipt?.ReceiptRows.Select(x => x.ItemRef) ?? []).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.ItemID))
            .Select(x => new { x.ItemID, x.Code, x.Title }).ToDictionaryAsync(x => x.ItemID, ct);
        string Label(int id) => items.TryGetValue(id, out var item) ? $"{item.Code} - {item.Title}" : id.ToString();
        var existing = oldRows.Select(x => new SupplySyncDestinationRow(x.ItemRef, Label(x.ItemRef), x.Description, PurchaseAmounts(x))).ToArray();
        var currentReceipt = receipt?.ReceiptRows.Select(x => new SupplySyncDestinationRow(x.ItemRef, Label(x.ItemRef),
            x.Description, ReceiptAmounts(x))).ToArray() ?? [];
        var sourceRows = source.Items.Select(row =>
        {
            var amounts = SourceAmounts(row);
            var matchingTitles = oldRows.Where(x => items.TryGetValue(x.ItemRef, out var item) &&
                NormalizeSupplySyncTitle(item.Title) == NormalizeSupplySyncTitle(row.Product)).ToArray();
            var exactMatches = (matchingTitles.Length > 0 ? matchingTitles : oldRows.AsEnumerable())
                .Where(x => PurchaseAmounts(x) == amounts).ToArray();
            var matches = exactMatches.Length > 0 ? exactMatches : matchingTitles;
            var suggestedIds = matches.Select(x => x.ItemRef).Distinct().Take(2).ToArray();
            return new SupplySyncSourceRow(row.Id, $"{row.ProductSmallName} {row.Product}".Trim(), row.UnitTxt, amounts,
                suggestedIds.Length == 1 ? suggestedIds[0] : null, matches.Length == 1 ? matches[0].Description : null);
        }).ToArray();
        var stockTitle = receipt?.Receipt is { } stockReceipt
            ? await db.Stocks.AsNoTracking().Where(x => x.StockID == stockReceipt.StockRef)
                .Select(x => x.Code + " - " + x.Title).SingleOrDefaultAsync(ct) : null;
        var vendorId = await db.Parties.AsNoTracking().Where(x => x.DLRef == invoice.VendorDLRef && x.IsVendor)
            .OrderBy(x => x.PartyId).Select(x => (int?)x.PartyId).FirstOrDefaultAsync(ct);
        if (!receiptOnly && !vendorId.HasValue)
            blockers.Add("تأمین‌کننده فاکتور خرید در سپیدار معتبر نیست.");
        var token = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            purchase.Token, ReceiptToken = receipt?.Token, receiptOnly, source.Id, source.DateMiladi, source.SerialNumber,
            source.SupplierId, source.SumTotalAmount, source.Discount, source.TotalTax, source.PaymentAmount, vendorId, sourceRows, blockers
        })));
        return new(purchase, receipt, new(token, invoice.Number, receipt?.Receipt?.Number, stockTitle, receipt?.Voucher?.Number,
            vendorId.GetValueOrDefault(), invoice.Description, blockers, sourceRows, existing, currentReceipt));
    }

    private static bool PurchaseChanged(SupplyFactor source, InventoryPurchaseInvoice invoice,
        IEnumerable<InventoryPurchaseInvoiceItem> rows) =>
        invoice.TotalPrice.GetValueOrDefault() != source.SumTotalAmount || invoice.TotalTax.GetValueOrDefault() != source.TotalTax ||
        invoice.TotalNetPrice.GetValueOrDefault() != source.PaymentAmount ||
        !source.Items.Select(SourceAmounts).SequenceEqual(rows.Select(PurchaseAmounts));

    private static bool ReceiptChanged(InventoryPurchaseInvoice invoice, IReadOnlyList<InventoryPurchaseInvoiceItem> rows,
        InventoryReceipt receipt, IReadOnlyList<InventoryReceiptItem> receiptRows)
    {
        if (!IsFullReceipt(rows, receiptRows))
            return true;
        var byId = receiptRows.ToDictionary(x => x.BasePurchaseInvoiceItemRef!.Value);
        return receipt.TotalNetPrice != invoice.TotalNetPrice || receipt.TotalTax.GetValueOrDefault() != rows.Sum(x => x.Tax.GetValueOrDefault()) ||
            receipt.TotalPrice.GetValueOrDefault() != rows.Sum(x => x.Price - x.Discount.GetValueOrDefault()) ||
            rows.Any(x => byId[x.InventoryPurchaseInvoiceItemID].Price.GetValueOrDefault() != x.Price - x.Discount.GetValueOrDefault() ||
                byId[x.InventoryPurchaseInvoiceItemID].Tax.GetValueOrDefault() != x.Tax.GetValueOrDefault());
    }

    private static bool IsFullReceipt(IReadOnlyList<InventoryPurchaseInvoiceItem> rows, IReadOnlyList<InventoryReceiptItem> receiptRows)
    {
        if (rows.Count == 0 || rows.Count != receiptRows.Count || receiptRows.Any(x => !x.BasePurchaseInvoiceItemRef.HasValue) ||
            receiptRows.Select(x => x.BasePurchaseInvoiceItemRef).Distinct().Count() != rows.Count)
            return false;
        var byId = receiptRows.ToDictionary(x => x.BasePurchaseInvoiceItemRef!.Value);
        return rows.All(x => x.RemainingQuantity == 0 && x.RemainingSecondaryQuantity.GetValueOrDefault() == 0 &&
            byId.TryGetValue(x.InventoryPurchaseInvoiceItemID, out var received) &&
            received.ItemRef == x.ItemRef && received.Quantity == x.Quantity && received.SecondaryQuantity == x.SecondaryQuantity);
    }

    private static bool UnsupportedPurchaseRow(InventoryPurchaseInvoiceItem row) =>
        row.CurrencyRate != 1m || row.TracingRef.HasValue || row.SecondaryQuantity.HasValue ||
        row.CostServiceAccountSLRef.HasValue || row.Addition.GetValueOrDefault() != 0 || row.Duty.GetValueOrDefault() != 0 ||
        row.TransportPrice.GetValueOrDefault() != 0 || row.TransportTax.GetValueOrDefault() != 0 || row.TransportDuty.GetValueOrDefault() != 0 ||
        row.InsuranceAmount.GetValueOrDefault() != 0 || row.WithHoldingTaxAmount.GetValueOrDefault() != 0 ||
        !string.IsNullOrWhiteSpace(row.Description_En) || !string.IsNullOrWhiteSpace(row.TransportDescription);

    private static bool UnsupportedReceiptRow(InventoryReceiptItem row) =>
        row.TracingRef.HasValue || row.SecondaryQuantity.HasValue || row.CurrencyRef.HasValue ||
        row.CurrencyRate.GetValueOrDefault(1m) != 1m || row.CurrencyValue.GetValueOrDefault() != 0 ||
        row.Duty.GetValueOrDefault() != 0 || row.TransportPrice.GetValueOrDefault() != 0 || row.TransportTax.GetValueOrDefault() != 0 ||
        row.TransportDuty.GetValueOrDefault() != 0 || row.OtherCostsAmount.GetValueOrDefault() != 0 || row.ImportOrderFinalFee.GetValueOrDefault() != 0 ||
        row.AllotmenedOtherCostInBaseCurrency.GetValueOrDefault() != 0 || row.ReturnedPrice.GetValueOrDefault() != 0 ||
        row.ReturnedNetPrice.GetValueOrDefault() != 0 || !string.IsNullOrWhiteSpace(row.Description_En) ||
        !string.IsNullOrWhiteSpace(row.TransportDescription) || !string.IsNullOrWhiteSpace(row.ParityCheck);

    private static SupplySyncAmounts SourceAmounts(SupplyFactorItem row) =>
        new(row.WeightCount, row.UnitAmount, row.TotalAmount + row.Discount - row.TaxAmount, row.Discount, row.TaxAmount, row.TotalAmount);
    private static SupplySyncAmounts PurchaseAmounts(InventoryPurchaseInvoiceItem row) =>
        new(row.Quantity, row.Fee, row.Price, row.Discount.GetValueOrDefault(), row.Tax.GetValueOrDefault(), row.NetPrice.GetValueOrDefault());
    private static SupplySyncAmounts ReceiptAmounts(InventoryReceiptItem row) =>
        new(row.Quantity, row.Fee.GetValueOrDefault(), row.Price.GetValueOrDefault(), 0m, row.Tax.GetValueOrDefault(), row.NetPrice.GetValueOrDefault());

    private static string NormalizeSupplySyncTitle(string? title) =>
        (title ?? "").Trim().Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک');

    private static void SetReceiptTotals(InventoryReceipt receipt, IReadOnlyList<InventoryPurchaseInvoiceItem> rows, decimal? total)
    {
        var price = rows.Sum(x => x.Price - x.Discount.GetValueOrDefault());
        var tax = rows.Sum(x => x.Tax.GetValueOrDefault());
        if (price < 0m || tax < 0m || price + tax != total ||
            decimal.Round(price, 3) != price || decimal.Round(tax, 3) != tax)
            throw new InvalidOperationException("جمع مبالغ رسید انبار با فاکتور خرید تطبیق ندارد.");
        receipt.TotalPrice = price;
        receipt.TotalTax = tax;
        receipt.TotalNetPrice = total;
    }

    private static Dictionary<int, string> MatchReceiptDescriptions(IReadOnlyList<InventoryPurchaseInvoiceItem> oldRows,
        IReadOnlyList<InventoryReceiptItem> receiptRows, IReadOnlyList<InventoryPurchaseInvoiceItem> replacement)
    {
        var notes = new Dictionary<int, string>();
        if (receiptRows.Count == 0)
            return notes;
        var received = receiptRows.ToDictionary(x => x.BasePurchaseInvoiceItemRef!.Value);
        foreach (var group in oldRows.GroupBy(x => x.ItemRef))
        {
            var old = group.ToArray();
            var next = replacement.Where(x => x.ItemRef == group.Key).ToArray();
            if (old.Length == 1 && next.Length == 1 && received[old[0].InventoryPurchaseInvoiceItemID].Description is { } description)
            {
                notes[next[0].RowNumber] = description;
                continue;
            }
            foreach (var row in old)
            {
                var note = received[row.InventoryPurchaseInvoiceItemID].Description;
                if (string.IsNullOrWhiteSpace(note))
                    continue;
                var amounts = PurchaseAmounts(row);
                var matches = next.Where(x => x.Quantity == amounts.Quantity && x.Fee == amounts.Fee && x.Price == amounts.Price &&
                    x.Discount.GetValueOrDefault() == amounts.Discount && x.Tax.GetValueOrDefault() == amounts.Tax).Take(2).ToArray();
                if (matches.Length == 1 && !notes.TryAdd(matches[0].RowNumber, note))
                    throw new InvalidOperationException("تطبیق توضیحات اقلام تکراری رسید ممکن نیست. ابتدا رسید انبار را بررسی کنید.");
                if (matches.Length > 1)
                    throw new InvalidOperationException("تطبیق توضیحات اقلام تکراری رسید ممکن نیست. ابتدا رسید انبار را بررسی کنید.");
            }
        }
        return notes;
    }

    private static async Task<ReceiptStockDelta> PrepareReceiptStockDeltaAsync(SepidarContextDbBase db, InventoryReceipt receipt,
        IReadOnlyList<InventoryReceiptItem> previous, IReadOnlyList<InventoryPurchaseInvoiceItem> replacement, CancellationToken ct)
    {
        var stock = receipt.StockRef;
        var year = receipt.FiscalYearRef;
        var nextItems = replacement.Select(row => row.ItemRef).Distinct().ToArray();
        var eligible = await db.ItemStocks.AsNoTracking().Where(x => x.StockRef == stock && x.Stock.IsActive && nextItems.Contains(x.ItemRef))
            .Select(x => x.ItemRef).Distinct().CountAsync(ct);
        if (eligible != nextItems.Length)
            throw new InvalidOperationException("انبار فعلی رسید برای همهٔ اقلام جدید معتبر نیست.");
        var delta = previous.Select(x => (x.ItemRef, Quantity: -x.Quantity))
            .Concat(replacement.Select(x => (x.ItemRef, x.Quantity))).GroupBy(x => x.ItemRef)
            .ToDictionary(x => x.Key, x => x.Sum(row => row.Quantity));
        using var summary = SepidarStockSummary.CreateTable(delta.Keys.Select(id => (stock, id, (int?)null)), year);
        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var before = await ReadStockAsync(db, receipt, delta.Keys.ToArray(), ct);
        return new(delta, before);
    }

    private static async Task VerifyReceiptStockDeltaAsync(SepidarContextDbBase db, InventoryReceipt receipt,
        ReceiptStockDelta stock, CancellationToken ct)
    {
        var itemIds = stock.Delta.Keys.ToArray();
        using var summary = SepidarStockSummary.CreateTable(itemIds.Select(id => (receipt.StockRef, id, (int?)null)), receipt.FiscalYearRef);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var after = await ReadStockAsync(db, receipt, itemIds, ct);
        foreach (var (itemId, quantity) in stock.Delta)
        {
            var key = (itemId, (int?)null);
            var current = after.GetValueOrDefault(key);
            if (current - stock.Before.GetValueOrDefault(key) != quantity || (quantity < 0m && current < 0m))
                throw new InvalidOperationException("همگام‌سازی باعث موجودی منفی یا مغایرت انبار می‌شود. هیچ تغییری ذخیره نشد.");
        }
    }

    private static async Task<bool> HasSupplySyncRowReferencesAsync(SepidarContextDbBase db, int purchaseId, int? receiptId, CancellationToken ct)
    {
        var references = await db.Database.SqlQuery<SupplySyncReference>($"""
            SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) AS SchemaName, OBJECT_NAME(f.parent_object_id) AS TableName,
                   COL_NAME(f.parent_object_id, f.parent_column_id) AS ColumnName,
                   OBJECT_NAME(f.referenced_object_id) AS TargetTableName,
                   COL_NAME(f.referenced_object_id, f.referenced_column_id) AS TargetColumnName
            FROM sys.foreign_key_columns f
            WHERE f.referenced_object_id IN (OBJECT_ID(N'INV.InventoryPurchaseInvoiceItem'), OBJECT_ID(N'INV.InventoryReceiptItem'))
            """).ToListAsync(ct);
        using var identifiers = new SqlCommandBuilder();
        foreach (var reference in references)
        {
            var isReceiptRow = reference.TargetTableName == "InventoryReceiptItem";
            if (isReceiptRow && receiptId == null)
                continue;
            var isRebuiltReceiptReference = !isReceiptRow && reference is {
                SchemaName: "INV", TableName: "InventoryReceiptItem", ColumnName: "BasePurchaseInvoiceItemRef"
            } && receiptId.HasValue;
            var sourceTable = identifiers.QuoteIdentifier(reference.SchemaName) + "." + identifiers.QuoteIdentifier(reference.TableName);
            var destinationTable = "INV." + identifiers.QuoteIdentifier(reference.TargetTableName);
            var column = identifiers.QuoteIdentifier(reference.ColumnName);
            var targetColumn = identifiers.QuoteIdentifier(reference.TargetColumnName);
            var parent = isReceiptRow ? "InventoryReceiptRef" : "InventoryPurchaseInvoiceRef";
            var excluded = isRebuiltReceiptReference ? " AND r.InventoryReceiptRef<>@receiptId" : "";
            var sql = $"SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM {sourceTable} r JOIN {destinationTable} d ON r.{column}=d.{targetColumn}" +
                $" WHERE d.{parent}=@id{excluded}) THEN 1 ELSE 0 END AS bit) AS [Value]";
            var id = new SqlParameter("id", isReceiptRow ? receiptId!.Value : purchaseId);
            object[] parameters = isRebuiltReceiptReference ? [id, new SqlParameter("receiptId", receiptId!.Value)] : [id];
            if (await db.Database.SqlQueryRaw<bool>(sql, parameters).SingleAsync(ct))
                return true;
        }
        return false;
    }

    private sealed record ReceiptStockDelta(Dictionary<int, decimal> Delta, Dictionary<(int ItemId, int? TracingId), decimal> Before);
    private sealed record SupplySyncReference(string SchemaName, string TableName, string ColumnName, string TargetTableName, string TargetColumnName);
    private sealed record SupplySyncPlan(MaintenancePlan Purchase, MaintenancePlan? Receipt, SupplySyncPreview Preview);
}
