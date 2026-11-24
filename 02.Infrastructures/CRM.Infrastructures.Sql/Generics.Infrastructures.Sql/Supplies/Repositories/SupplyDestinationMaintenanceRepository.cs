using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;

public sealed partial class SupplyDestinationMaintenanceRepository(Sepidar1ContextDb first, Sepidar2ContextDb second)
    : ISupplyDestinationMaintenanceRepository
{
    private const string PurchaseType = "SG.Inventory.InventoryOperations.Common.DsInventoryPurchaseInvoice";
    private const string ReceiptType = "SG.Inventory.InventoryOperations.Common.DsInventoryReceipt";
    private const string VoucherType = "SG.Accounting.VoucherManagement.Common.DsVoucher";
    private const string LinkedReceipt = "ابتدا رسید انبار وابسته به این فاکتور خرید را حذف کنید.";

    public async Task<SupplyPurchaseEditState> GetPurchaseAsync(SupplyDestinationDocument document, CancellationToken ct)
    {
        var db = Resolve(document);
        var plan = await BuildPlanAsync(db, document, false, ct);
        var vendorId = await db.Parties.AsNoTracking().Where(x => x.DLRef == plan.Purchase.VendorDLRef && x.IsVendor)
            .OrderBy(x => x.PartyId).Select(x => (int?)x.PartyId).FirstOrDefaultAsync(ct);
        var vendorName = await db.DLs.AsNoTracking().Where(x => x.DLId == plan.Purchase.VendorDLRef)
            .Select(x => x.Title).SingleOrDefaultAsync(ct);
        return new(plan.Purchase.Number, plan.Purchase.Date, vendorId ?? 0, vendorName,
            plan.Purchase.Description, plan.PurchaseRows.Select(row => new SupplyPurchaseEditRow(
                row.RowNumber, row.ItemRef, row.Quantity, row.Fee, row.Description)).ToArray(),
            plan.Token, plan.Blockers.FirstOrDefault(), plan.HasLinkedReceipt);
    }

    public async Task EditPurchaseAsync(SupplyDestinationDocument document, string token, int modifier,
        SupplyPurchaseEdit edit, CancellationToken ct)
    {
        var db = Resolve(document);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await BuildPlanAsync(db, document, true, ct);
        ValidatePlan(plan, token, deleting: false);
        var rows = edit.Rows.OrderBy(row => row.RowNumber).ToArray();
        if (!plan.PurchaseRows.Select(row => row.RowNumber).SequenceEqual(rows.Select(row => row.RowNumber)))
            throw new InvalidOperationException("ردیف‌های فاکتور تغییر کرده‌اند. فرم ویرایش را دوباره باز کنید.");
        int? vendorDlId = null;
        if (plan.HasLinkedReceipt)
        {
            if (edit.VendorId.HasValue || !plan.PurchaseRows.Select(row => row.ItemRef).SequenceEqual(rows.Select(row => row.ItemId)))
                throw new InvalidOperationException("به دلیل ثبت رسید انبار، فقط توضیحات فاکتور خرید قابل ویرایش است.");
        }
        else
        {
            vendorDlId = await db.Parties.AsNoTracking()
                .Where(x => x.PartyId == edit.VendorId && x.IsVendor && x.Dl != null)
                .Select(x => (int?)x.DLRef).SingleOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("تأمین‌کننده انتخاب‌شده در سپیدار معتبر نیست.");
            await ValidateItemReplacementsAsync(db, plan.PurchaseRows, rows, ct);
        }
        var purchase = plan.Purchase;
        db.Attach(purchase);
        if (vendorDlId is int newVendorDlId && purchase.VendorDLRef != newVendorDlId)
        {
            purchase.PartyAddressRef = null;
            purchase.VendorDLRef = newVendorDlId;
        }
        purchase.Description = edit.Description;
        purchase.LastModifier = modifier;
        purchase.LastModificationDate = DateTime.Now;
        purchase.Version++;

        for (var i = 0; i < rows.Length; i++)
        {
            var current = plan.PurchaseRows[i];
            var updated = rows[i];
            if (current.ItemRef == updated.ItemId && current.Description == updated.Description)
                continue;
            db.Attach(current);
            current.ItemRef = updated.ItemId;
            current.Description = updated.Description;
            current.Version++;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<SupplyDestinationDeletion> PreviewDeletionAsync(SupplyDestinationDocument document, CancellationToken ct)
    {
        var plan = await BuildPlanAsync(Resolve(document), document, false, ct);
        return new(plan.Receipt?.Number ?? plan.Purchase.Number, plan.Voucher?.Number, plan.Token, plan.DeletionBlockers);
    }

    private static async Task ValidateItemReplacementsAsync(SepidarContextDbBase db,
        IReadOnlyList<InventoryPurchaseInvoiceItem> current, SupplyPurchaseEditRow[] updated, CancellationToken ct)
    {
        var replacements = current.Zip(updated).Where(pair => pair.First.ItemRef != pair.Second.ItemId).ToArray();
        if (replacements.Length == 0)
            return;
        var ids = replacements.SelectMany(pair => new[] { pair.First.ItemRef, pair.Second.ItemId }).Distinct().ToArray();
        var items = await (from item in db.Items.AsNoTracking()
                           join unit in db.Units.AsNoTracking() on item.UnitRef equals unit.UnitID
                           where ids.Contains(item.ItemID)
                           select new {
                               item.ItemID, unit.Title,
                               Eligible = item.IsActive && item.Type == 1 && !item.SecondaryUnitRef.HasValue &&
                                   !item.SerialTracking && !item.IsPricingBasedOnTracing
                           }).ToDictionaryAsync(x => x.ItemID, ct);
        foreach (var (previous, next) in replacements)
        {
            if (previous.TracingRef.HasValue || previous.SecondaryQuantity.HasValue)
                throw new InvalidOperationException($"کالای ردیف {next.RowNumber} دارای ردیابی یا واحد فرعی است و قابل تغییر نیست.");
            if (!items.TryGetValue(next.ItemId, out var selected) || !selected.Eligible)
                throw new InvalidOperationException($"کالای انتخاب‌شده در ردیف {next.RowNumber} در سپیدار معتبر نیست.");
            if (!SupplyPurchaseUnit.Matches(items.GetValueOrDefault(previous.ItemRef)?.Title, selected.Title))
                throw new InvalidOperationException($"واحد کالای معادل ردیف {next.RowNumber} با واحد فعلی یکسان نیست.");
        }
    }

    public async Task DeleteAsync(SupplyDestinationDocument document, string token, CancellationToken ct)
    {
        var db = Resolve(document);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await BuildPlanAsync(db, document, true, ct);
        ValidatePlan(plan, token, deleting: true);
        try
        {
            if (plan.Receipt == null)
            {
                await DeleteSupplementaryDataAsync(db, PurchaseType, document.PurchaseId, ct);
                await db.InventoryPurchaseInvoices.Where(x => x.InventoryPurchaseInvoiceID == document.PurchaseId)
                    .ExecuteDeleteAsync(ct);
            }
            else
            {
                await DeleteReceiptAsync(db, plan, ct);
            }
            await transaction.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new InvalidOperationException("سند وابسته‌ای در سپیدار مانع حذف است. هیچ‌یک از اسناد این عملیات حذف نشد.", ex);
        }
    }

    private SepidarContextDbBase Resolve(SupplyDestinationDocument document) =>
        document.SoftwareId == Sepidar1SoftwareId ? first :
        document.SoftwareId == Sepidar2SoftwareId ? second :
        throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");

    private static void ValidatePlan(MaintenancePlan plan, string token, bool deleting)
    {
        var blockers = deleting ? plan.DeletionBlockers : plan.Blockers;
        if (blockers.Count > 0)
            throw new InvalidOperationException(string.Join(" ", blockers));
        if (string.IsNullOrEmpty(token) || token != plan.Token)
            throw new InvalidOperationException("اطلاعات سند تغییر کرده است. دوباره آن را بررسی کنید.");
    }

    private static async Task<MaintenancePlan> BuildPlanAsync(SepidarContextDbBase db, SupplyDestinationDocument document,
        bool forUpdate, CancellationToken ct)
    {
        var purchaseId = document.PurchaseId;
        var purchaseQuery = forUpdate
            ? db.InventoryPurchaseInvoices.FromSql($"SELECT * FROM INV.InventoryPurchaseInvoice WITH (UPDLOCK,HOLDLOCK) WHERE InventoryPurchaseInvoiceID={purchaseId}")
            : db.InventoryPurchaseInvoices.AsQueryable();
        var purchase = await purchaseQuery.AsNoTracking().SingleOrDefaultAsync(x =>
            x.InventoryPurchaseInvoiceID == purchaseId && x.Type == 1, ct)
            ?? throw new InvalidOperationException("فاکتور خرید در نرم‌افزار مقصد پیدا نشد.");
        var rows = await db.InventoryPurchaseInvoiceItems.AsNoTracking().Where(x => x.InventoryPurchaseInvoiceRef == purchaseId)
            .OrderBy(x => x.RowNumber).ThenBy(x => x.InventoryPurchaseInvoiceItemID).ToListAsync(ct);
        var rowIds = rows.Select(row => row.InventoryPurchaseInvoiceItemID).ToArray();
        var blockers = new List<string>();
        if (document.ReceiptId is not int receiptId)
        {
            var hasLinkedReceipt = await db.InventoryReceipts.AnyAsync(x => x.BasePurchaseInvoiceRef == purchaseId, ct) ||
                await db.InventoryReceiptItems.AnyAsync(x =>
                    (x.BasePurchaseInvoiceItemRef.HasValue && rowIds.Contains(x.BasePurchaseInvoiceItemRef.Value)) ||
                    (x.ServiceInventoryPurchaseInvoiceRef.HasValue && rowIds.Contains(x.ServiceInventoryPurchaseInvoiceRef.Value)), ct);
            if (purchase.PaymentHeaderRef.HasValue || purchase.AccountingVoucherRef.HasValue || purchase.PurchaseOrderRef.HasValue ||
                rows.Any(row => row.PurchaseOrderItemRef.HasValue))
                blockers.Add("فاکتور خرید به سند حسابداری، پرداخت یا سفارش خرید وابسته است.");
            if (await HasExternalPurchaseDependenciesAsync(db, purchaseId, ct))
                blockers.Add("فاکتور خرید یا اقلام آن در اسناد دیگر سپیدار استفاده شده‌اند.");
            await CheckFiscalYearAsync(db, purchase.FiscalYearRef, blockers, ct);
            return new(purchase, rows, null, [], null, blockers, hasLinkedReceipt);
        }

        var receiptQuery = forUpdate
            ? db.InventoryReceipts.FromSql($"SELECT * FROM INV.InventoryReceipt WITH (UPDLOCK,HOLDLOCK) WHERE InventoryReceiptID={receiptId}")
            : db.InventoryReceipts.AsQueryable();
        var receipt = await receiptQuery.AsNoTracking().SingleOrDefaultAsync(x =>
            x.InventoryReceiptID == receiptId && x.BasePurchaseInvoiceRef == purchaseId && !x.IsReturn, ct)
            ?? throw new InvalidOperationException("رسید انبار مرتبط با این فاکتور خرید در سپیدار پیدا نشد.");
        var receiptRows = await db.InventoryReceiptItems.AsNoTracking().Where(x => x.InventoryReceiptRef == receiptId)
            .OrderBy(x => x.InventoryReceiptItemID).ToListAsync(ct);
        var receiptRowIds = receiptRows.Select(x => x.InventoryReceiptItemID).ToArray();
        if (receipt.Type != 1 || receipt.PurchaseType != 1 || receipt.CreatorForm != 1 || receipt.IsWastage ||
            receipt.BaseInventoryDeliveryRef.HasValue || receipt.BaseImportPurchaseInvoiceRef.HasValue || receipt.PaymentHeaderRef.HasValue ||
            receiptRows.Count == 0 || receiptRows.Any(row => row.IsReturn || row.ReturnBase.HasValue ||
                row.BasePurchaseInvoiceItemRef is not int rowId || !rowIds.Contains(rowId) ||
                row.InventoryDeliveryItemRef.HasValue || row.ProductOrderRef.HasValue || row.WeighingRef.HasValue ||
                row.ServiceInventoryPurchaseInvoiceRef.HasValue || row.BaseImportPurchaseInvoiceItemRef.HasValue))
            blockers.Add("رسید انبار دارای اقلام یا وابستگی‌های خارج از گردش فاکتور خرید است.");
        if (await db.InventoryReceiptItems.AnyAsync(x => x.ReturnBase.HasValue && receiptRowIds.Contains(x.ReturnBase.Value), ct))
            blockers.Add("برای این رسید انبار برگشت ثبت شده است.");
        if (await HasExternalReceiptDependenciesAsync(db, receipt, ct))
            blockers.Add("این رسید در قیمت‌گذاری انبار، گزارش مالیاتی یا سند دیگری استفاده شده است.");
        await CheckFiscalYearAsync(db, purchase.FiscalYearRef, blockers, ct);
        if (receipt.FiscalYearRef != purchase.FiscalYearRef)
            await CheckFiscalYearAsync(db, receipt.FiscalYearRef, blockers, ct);

        Voucher? voucher = null;
        if (receipt.AccountingVoucherRef is int voucherId)
        {
            voucher = await db.Vouchers.AsNoTracking().SingleOrDefaultAsync(x => x.VoucherId == voucherId, ct);
            var voucherLines = db.VoucherItems.AsNoTracking().Where(x => x.VoucherRef == voucherId);
            if (voucher == null || voucher.State != 1 || voucher.Type != 2 || voucher.IssuerSystem != 1 ||
                voucher.IsMerged || voucher.FiscalYearRef != receipt.FiscalYearRef ||
                !await voucherLines.AnyAsync(ct) || await voucherLines.AnyAsync(x => x.IssuerEntityRef != receiptId ||
                    x.IssuerEntityName == null || !(x.IssuerEntityName == ReceiptType || x.IssuerEntityName.StartsWith(ReceiptType + ",")), ct) ||
                await db.InventoryReceipts.AnyAsync(x => x.AccountingVoucherRef == voucherId && x.InventoryReceiptID != receiptId, ct) ||
                await db.Database.SqlQuery<bool>($"""
                    SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM ACC.MergedVoucherReferenceNumber WHERE VoucherRef={voucherId})
                        THEN 1 ELSE 0 END AS bit) AS [Value]
                    """).SingleAsync(ct))
                blockers.Add("سند حسابداری مرتبط، موقت و مختص این رسید انبار نیست.");
        }

        return new(purchase, rows, receipt, receiptRows, voucher, blockers);
    }

    private static async Task CheckFiscalYearAsync(SepidarContextDbBase db, int id, List<string> blockers, CancellationToken ct)
    {
        const string message = "سال مالی سند در سپیدار باز نیست.";
        if (!blockers.Contains(message) && !await db.FiscalYears.AnyAsync(x => x.FiscalYearId == id && x.Status == 1, ct))
            blockers.Add(message);
    }

    private static Task<bool> HasExternalPurchaseDependenciesAsync(SepidarContextDbBase db, int id, CancellationToken ct) =>
        db.Database.SqlQuery<bool>($"""
            SELECT CAST(CASE WHEN
                EXISTS (SELECT 1 FROM CNT.ContractRelatedPurchaseInvoice WHERE PurchaseInvoiceRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.BillItem WHERE ServiceInventoryPurchaseInvoiceRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.MarketingDisketteItem WHERE ServiceInventoryPurchaseInvoiceRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.MarketingDisketteItem1396 WHERE ServiceInventoryPurchaseInvoiceRef={id} OR AssetPurchaseInvoiceRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.VatItem WHERE ServiceInventoryPurchaseInvoiceRef={id} OR AssetPurchaseInvoiceRef={id}) OR
                EXISTS (SELECT 1 FROM INV.InventoryPurchaseInvoiceItem p WHERE p.InventoryPurchaseInvoiceRef={id} AND (
                    EXISTS (SELECT 1 FROM AST.AssetRelatedPurchaseInvoice WHERE AssetPurchaseInvoiceItemRef=p.InventoryPurchaseInvoiceItemID) OR
                    EXISTS (SELECT 1 FROM INV.InventoryReceiptOtherCostItem WHERE ServiceInventoryPurchaseInvoiceItemRef=p.InventoryPurchaseInvoiceItemID) OR
                    EXISTS (SELECT 1 FROM POM.PurchaseOtherCostItem WHERE ServiceInventoryPurchaseInvoiceItemRef=p.InventoryPurchaseInvoiceItemID)))
                THEN 1 ELSE 0 END AS bit) AS [Value]
            """).SingleAsync(ct);

    private static Task<bool> HasExternalReceiptDependenciesAsync(SepidarContextDbBase db, InventoryReceipt receipt, CancellationToken ct)
    {
        var id = receipt.InventoryReceiptID;
        return db.Database.SqlQuery<bool>($"""
            SELECT CAST(CASE WHEN
                EXISTS (SELECT 1 FROM INV.InventoryPricing WHERE StockRef={receipt.StockRef} AND FiscalYearRef={receipt.FiscalYearRef} AND EndDate >= {receipt.Date}) OR
                EXISTS (SELECT 1 FROM INV.InventoryBalancing WHERE InventoryReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM INV.InventoryReceiptOtherCostItem WHERE InventoryReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM CNT.ContractEmployerMaterialsItem WHERE ReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.BillItem WHERE InventoryReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.MarketingDisketteItem WHERE InventoryReceiptRef={id} OR ReturnedInventoryReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.MarketingDisketteItem1396 WHERE InventoryReceiptRef={id} OR ReturnedInventoryReceiptRef={id}) OR
                EXISTS (SELECT 1 FROM GNR.VatItem WHERE InventoryReceiptRef={id} OR InventoryReceiptReturnRef={id})
                THEN 1 ELSE 0 END AS bit) AS [Value]
            """).SingleAsync(ct);
    }

    private static async Task DeleteReceiptAsync(SepidarContextDbBase db, MaintenancePlan plan, CancellationToken ct)
    {
        var receipt = plan.Receipt!;
        var changes = plan.ReceiptRows.GroupBy(row => (receipt.StockRef, row.ItemRef, row.TracingRef))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));
        using var summary = SepidarStockSummary.CreateTable(changes.Keys, receipt.FiscalYearRef);
        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var itemIds = changes.Keys.Select(key => key.ItemRef).Distinct().ToArray();
        var before = await ReadStockAsync(db, receipt, itemIds, ct);

        await DeleteSupplementaryDataAsync(db, ReceiptType, receipt.InventoryReceiptID, ct);
        await db.InventoryReceipts.Where(x => x.InventoryReceiptID == receipt.InventoryReceiptID).ExecuteDeleteAsync(ct);
        if (plan.Voucher is { } voucher)
        {
            await DeleteSupplementaryDataAsync(db, VoucherType, voucher.VoucherId, ct);
            await db.Vouchers.Where(x => x.VoucherId == voucher.VoucherId).ExecuteDeleteAsync(ct);
        }

        var affected = plan.ReceiptRows.Select(x => x.BasePurchaseInvoiceItemRef!.Value).Distinct().ToArray();
        await RestorePurchaseBalancesAsync(db, plan.PurchaseRows.Where(x => affected.Contains(x.InventoryPurchaseInvoiceItemID)),
            affected, ct);
        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var after = await ReadStockAsync(db, receipt, itemIds, ct);
        foreach (var change in changes)
        {
            var key = (change.Key.ItemRef, change.Key.TracingRef);
            var previous = before.GetValueOrDefault(key);
            var current = after.GetValueOrDefault(key);
            if (current < 0 || previous - current != change.Value)
                throw new InvalidOperationException("حذف رسید باعث موجودی منفی یا مغایرت موجودی انبار می‌شود.");
        }
    }

    private static async Task RestorePurchaseBalancesAsync(SepidarContextDbBase db,
        IEnumerable<InventoryPurchaseInvoiceItem> rows, int[] affected, CancellationToken ct)
    {
        var received = await db.InventoryReceiptItems.AsNoTracking()
            .Where(x => x.BasePurchaseInvoiceItemRef.HasValue && affected.Contains(x.BasePurchaseInvoiceItemRef.Value))
            .GroupBy(x => x.BasePurchaseInvoiceItemRef!.Value)
            .Select(g => new {
                Id = g.Key, Quantity = g.Sum(x => x.IsReturn ? -x.Quantity : x.Quantity),
                Secondary = g.Sum(x => (x.IsReturn ? -x.SecondaryQuantity : x.SecondaryQuantity) ?? 0m)
            }).ToDictionaryAsync(g => g.Id, ct);
        foreach (var row in rows)
        {
            var net = received.GetValueOrDefault(row.InventoryPurchaseInvoiceItemID);
            var remaining = row.Quantity - (net?.Quantity ?? 0m);
            var secondary = row.SecondaryQuantity - (net?.Secondary ?? 0m);
            if (remaining < 0m || remaining > row.Quantity || secondary < 0m || secondary > row.SecondaryQuantity)
                throw new InvalidOperationException("مانده اقلام فاکتور خرید با رسیدهای باقی‌مانده در سپیدار تطبیق ندارد.");
            await db.InventoryPurchaseInvoiceItems.Where(x => x.InventoryPurchaseInvoiceItemID == row.InventoryPurchaseInvoiceItemID)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.RemainingQuantity, remaining)
                    .SetProperty(x => x.RemainingSecondaryQuantity, secondary), ct);
        }
    }

    private static Task<Dictionary<(int ItemId, int? TracingId), decimal>> ReadStockAsync(
        SepidarContextDbBase db, InventoryReceipt receipt, int[] itemIds, CancellationToken ct) =>
        ReadStockAsync(db, receipt.FiscalYearRef, receipt.StockRef, itemIds, ct);

    private static async Task<Dictionary<(int ItemId, int? TracingId), decimal>> ReadStockAsync(
        SepidarContextDbBase db, int year, int stock, int[] itemIds, CancellationToken ct)
    {
        var rows = await db.ItemStockSummaries.AsNoTracking().Where(x => x.FiscalYearRef == year &&
                x.StockRef == stock && itemIds.Contains(x.ItemRef) && x.UnitRef == x.Item.UnitRef)
            .GroupBy(x => new { x.ItemRef, x.TracingRef })
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) }).ToListAsync(ct);
        return rows.ToDictionary(x => (x.Key.ItemRef, x.Key.TracingRef), x => x.Quantity);
    }

    private static async Task DeleteSupplementaryDataAsync(SepidarContextDbBase db, string type, int id, CancellationToken ct)
    {
        await db.ExtraData.Where(x => x.EntityRef == id && x.EntityTypeName == type).ExecuteDeleteAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM FMK.Note WHERE EntityRef={id} AND EntityType={type}", ct);
    }

    private sealed record MaintenancePlan(InventoryPurchaseInvoice Purchase, List<InventoryPurchaseInvoiceItem> PurchaseRows,
        InventoryReceipt? Receipt, List<InventoryReceiptItem> ReceiptRows, Voucher? Voucher, List<string> Blockers,
        bool HasLinkedReceipt = false)
    {
        public IReadOnlyList<string> DeletionBlockers => HasLinkedReceipt ? [LinkedReceipt, ..Blockers] : Blockers;

        public string Token => Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(new { Purchase, PurchaseRows, Receipt, ReceiptRows, HasLinkedReceipt,
                Voucher = Voucher == null ? null : new { Voucher.VoucherId, Voucher.Number, Voucher.Version, Voucher.LastModificationDate }, Blockers })));
    }
}
