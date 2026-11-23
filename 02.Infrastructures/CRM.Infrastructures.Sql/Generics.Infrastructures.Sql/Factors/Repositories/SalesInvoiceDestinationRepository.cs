using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Repositories;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Framework;
using CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.Infrastructures.Sql.Factors.Repositories;

public sealed partial class SalesInvoiceDestinationRepository(CRMContextDb crm, Sepidar1ContextDb first, Sepidar2ContextDb second)
    : ISalesInvoiceDestinationRepository
{
    private const int OpenFiscalYear = 1;
    private const int ActiveInvoice = 1;
    private const int TemporaryVoucher = 1;
    private const int SystemVoucher = 2;
    private const int SalesIssuer = 0;
    private const int SalesDelivery = 1;
    private const int CreatedFromSales = 1;
    private const int TaxPayerBillAwaitingInquiry = 2;
    private const int TaxPayerBillSent = 4;
    private const string PendingDelete = "حذف فاکتور فروش در نرم‌افزار مالی، در انتظار ثبت نتیجه";
    private const string InvoiceType = "SG.Sales.InvoiceManagement.Common.DsInvoice";
    private const string DeliveryType = "SG.Inventory.InventoryOperations.Common.DsInventoryDelivery";
    private const string VoucherType = "SG.Accounting.VoucherManagement.Common.DsVoucher";

    public async Task<SalesInvoiceDeletionPreview> PreviewAsync(SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        var (db, mapping, id) = await ResolveAsync(key, ct);
        var plan = await BuildPlanAsync(db, id, lockInvoice: false, ct)
            ?? throw new SalesInvoiceDestinationException("فاکتور در نرم‌افزار مقصد پیدا نشد.");
        return new(plan.Invoice.Number, mapping.ExternalSoftware.Title, plan.Token,
            plan.Voucher == null ? [] : [plan.Voucher.Number],
            plan.Deliveries.Select(x => new SalesInvoiceDeliveryNumber(x.Number, x.StockCode)).ToArray(), plan.Blockers);
    }

    public async Task DeleteAsync(SalesInvoiceDestinationKey key, string token, string userId, string userName, CancellationToken ct)
    {
        await using var mappingTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (db, mapping, id) = await ResolveAsync(key, ct);
        await using var destinationTransaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await BuildPlanAsync(db, id, lockInvoice: true, ct);
        if (plan == null)
        {
            await mappingTransaction.RollbackAsync(ct);
            if (!await CompleteInterruptedDeletionAsync(key, ct))
                throw new SalesInvoiceDestinationException("فاکتور در مقصد پیدا نشد. از گزینهٔ حذف سابقه استفاده کنید.");
            return;
        }
        if (plan.Blockers.Count > 0)
            throw new SalesInvoiceDestinationException(string.Join("، ", plan.Blockers));
        if (plan.Token != token)
            throw new SalesInvoiceDestinationException("فاکتور یا اسناد وابسته تغییر کرده‌اند. اطلاعات حذف را دوباره بررسی کنید.");

        try
        {
            await DeleteDestinationAsync(db, plan, ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new SalesInvoiceDestinationException("سند وابسته‌ای در سپیدار مانع حذف است. فاکتور و اسنادش حذف نشدند.");
        }

        var audit = await crm.logSystems.SingleOrDefaultAsync(x => x.Id == mapping.Id, ct);
        if (audit == null)
        {
            audit = new LogSystem { Id = mapping.Id, EntityName = nameof(Invoice), EntityId = mapping.ExEntityId,
                ParentEntityName = nameof(Factor), ParentEntityId = mapping.EntityId.ToString() };
            crm.logSystems.Add(audit);
        }
        audit.EntitySerialNumber = plan.Invoice.Number;
        audit.ChangedById = userId;
        audit.ChangedBy = userName;
        audit.ChangeTime = DateTime.Now;
        audit.ChangeTimeTxt = BaseServices.GetDateTimePersian();
        audit.ActionType = LogActionEnum.تغییر_وضعیت;
        audit.ActionTypeTxt = "درخواست حذف";
        audit.CommentTxt = mapping.ExternalSoftware.Title;
        audit.Description = PendingDelete;
        var documents = plan.Deliveries.Select(x => $"خروج انبار شماره {x.Number} (انبار {x.StockCode})").ToList();
        if (plan.Voucher != null)
            documents.Insert(0, $"سند حسابداری فروش شماره {plan.Voucher.Number}");
        audit.ChangeDetails = string.Join("، ", documents);
        await crm.SaveChangesAsync(ct);
        // Keep a durable deletion intent in CRM before committing the separate Sepidar transaction.
        await mappingTransaction.CommitAsync(ct);
        await destinationTransaction.CommitAsync(ct);
        await CompleteDeletionAsync(mapping, audit, ct);
    }

    public async Task<bool> CompleteInterruptedDeletionAsync(SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        var audit = await crm.logSystems.SingleOrDefaultAsync(x => x.Id == key.MappingId &&
            x.EntityName == nameof(Invoice) && x.Description == PendingDelete, ct);
        if (audit == null)
            return false;
        var (db, mapping, id) = await ResolveAsync(key, ct);
        if (audit.EntityId != mapping.ExEntityId || audit.ParentEntityId != mapping.EntityId.ToString() ||
            await db.SalesInvoices.AnyAsync(x => x.InvoiceId == id, ct))
            return false;
        await CompleteDeletionAsync(mapping, audit, ct);
        return true;
    }

    private async Task CompleteDeletionAsync(ExternalMapping mapping, LogSystem audit, CancellationToken ct)
    {
        await using var transaction = await crm.Database.BeginTransactionAsync(ct);
        audit.ActionType = LogActionEnum.حذف;
        audit.ActionTypeTxt = "حذف";
        audit.Description = $"فاکتور فروش شماره {audit.EntitySerialNumber} در {audit.CommentTxt} حذف شد.";
        await crm.ExternalMappings.Where(x => x.Id == mapping.Id && x.EntityId == mapping.EntityId &&
            x.EntityName == mapping.EntityName && x.ExternalSoftwareId == mapping.ExternalSoftwareId &&
            x.MapType == mapping.MapType && x.ExEntityName == mapping.ExEntityName && x.ExEntityId == mapping.ExEntityId)
            .ExecuteDeleteAsync(ct);
        await crm.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<(SepidarContextDbBase Db, ExternalMapping Mapping, int Id)> ResolveAsync(
        SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        SepidarContextDbBase db = key.ExternalSoftwareId == Sepidar1SoftwareId ? first :
            key.ExternalSoftwareId == Sepidar2SoftwareId ? second :
            throw new SalesInvoiceDestinationException("نرم‌افزار مقصد معتبر نیست.");
        var mapping = await crm.ExternalMappings.Include(x => x.ExternalSoftware).SingleOrDefaultAsync(x =>
            x.Id == key.MappingId && x.EntityId == key.SourceId && x.ExternalSoftwareId == key.ExternalSoftwareId &&
            x.EntityName == nameof(Factor) && x.MapType == ExternalMappingTypeEnum.فاکتور, ct);
        if (mapping?.ExEntityName != nameof(Invoice) ||
            !int.TryParse(mapping.ExEntityId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0 ||
            !await crm.Factors.AnyAsync(x => x.Id == key.SourceId, ct))
            throw new SalesInvoiceDestinationException("سابقهٔ معتبر فاکتور فروش در ERP پیدا نشد.");
        if (await crm.ExternalMappings.AnyAsync(x => x.Id != mapping.Id && x.ExternalSoftwareId == key.ExternalSoftwareId &&
                x.ExEntityName == nameof(Invoice) && x.ExEntityId == mapping.ExEntityId, ct))
            throw new SalesInvoiceDestinationException("این فاکتور مقصد به بیش از یک سابقه در ERP مرتبط است.");
        return (db, mapping, id);
    }

    private static async Task<DeletionPlan?> BuildPlanAsync(SepidarContextDbBase db, int id, bool lockInvoice, CancellationToken ct)
    {
        var query = lockInvoice ? db.SalesInvoices.FromSql(
            $"SELECT * FROM SLS.Invoice WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId = {id}") : db.SalesInvoices.AsQueryable();
        var invoice = await query.AsNoTracking().Where(x => x.InvoiceId == id)
            .Select(x => new InvoiceHeader(x.InvoiceId, x.Number, x.Version, x.LastModificationDate, x.FiscalYearRef,
                x.State, x.BaseOnInventoryDelivery, x.OrderRef, x.VoucherRef)).SingleOrDefaultAsync(ct);
        if (invoice == null)
            return null;
        var items = await db.SalesInvoiceItems.AsNoTracking().Where(x => x.InvoiceRef == id).OrderBy(x => x.InvoiceItemID)
            .Select(x => new InvoiceLine(x.InvoiceItemID, x.ItemRef, x.StockRef, x.TracingRef, x.Quantity, x.QuotationItemRef,
                x.OrderItemRef)).ToListAsync(ct);
        var linkedDeliveryIds = from line in db.InventoryDeliveryItems
            join item in db.SalesInvoiceItems on line.BaseInvoiceItem equals (int?)item.InvoiceItemID
            where item.InvoiceRef == id
            select line.InventoryDeliveryRef;
        var deliveries = await db.InventoryDeliveries.AsNoTracking()
            .Where(x => linkedDeliveryIds.Contains(x.InventoryDeliveryID)).OrderBy(x => x.InventoryDeliveryID)
            .Select(x => new DeliveryHeader(x.InventoryDeliveryID, x.Number, x.StockRef,
                db.Stocks.Where(stock => stock.StockID == x.StockRef).Select(stock => (int?)stock.Code).FirstOrDefault() ?? x.StockRef,
                x.FiscalYearRef, x.Type, x.CreatorForm, x.IsReturn, x.AccountingVoucherRef, x.Version, x.LastModificationDate))
            .ToListAsync(ct);
        var deliveryLines = await db.InventoryDeliveryItems.AsNoTracking()
            .Where(x => linkedDeliveryIds.Contains(x.InventoryDeliveryRef)).OrderBy(x => x.InventoryDeliveryItemID)
            .Select(x => new DeliveryLine(x.InventoryDeliveryItemID, x.InventoryDeliveryRef, x.BaseInvoiceItem,
                x.ItemRef, x.TracingRef, x.Quantity, x.Version, x.IsReturn, x.BaseInventoryDeliveryItem))
            .ToListAsync(ct);
        var voucher = invoice.VoucherId == null ? null : await db.Vouchers.AsNoTracking().Where(x => x.VoucherId == invoice.VoucherId)
            .Select(x => new VoucherHeader(x.VoucherId, x.Number, x.FiscalYearRef, x.State, x.Type,
                x.IssuerSystem, x.IsMerged, x.Version, x.LastModificationDate)).SingleOrDefaultAsync(ct);
        var blockers = new List<string>();
        if (invoice.State != ActiveInvoice)
            blockers.Add("وضعیت فاکتور در سپیدار برای حذف از این صفحه پشتیبانی نمی‌شود.");
        if (!await db.FiscalYears.AnyAsync(x => x.FiscalYearId == invoice.Year && x.Status == OpenFiscalYear, ct))
            blockers.Add("سال مالی فاکتور در سپیدار باز نیست.");
        if (invoice.BaseOnDelivery || invoice.OrderId != null || items.Any(x => x.OrderItemId != null))
            blockers.Add("فاکتور از روی خروج انبار یا سفارش دیگری صادر شده است.");
        var itemIds = items.Select(x => x.Id).ToHashSet();
        if (deliveries.Any(x => x.Year != invoice.Year || x.Type != SalesDelivery || x.CreatorForm != CreatedFromSales || x.IsReturn) ||
            deliveryLines.Any(x => x.InvoiceItemId is not int itemId || !itemIds.Contains(itemId) || x.IsReturn || x.BaseDeliveryItemId != null))
            blockers.Add("یکی از خروج‌های انبار مستقل است یا ردیف مشترک با سند دیگری دارد.");
        if (deliveries.Count > 0)
        {
            var lineIds = deliveryLines.Select(x => x.Id).ToArray();
            var ids = deliveries.Select(x => x.Id).ToArray();
            if (await db.InventoryReceipts.AnyAsync(x => x.BaseInventoryDeliveryRef.HasValue &&
                    ids.Contains(x.BaseInventoryDeliveryRef.Value), ct) ||
                await db.InventoryReceiptItems.AnyAsync(x => x.InventoryDeliveryItemRef.HasValue &&
                    lineIds.Contains(x.InventoryDeliveryItemRef.Value), ct) ||
                await db.InventoryDeliveryItems.AnyAsync(x => x.BaseInventoryDeliveryItem.HasValue &&
                    lineIds.Contains(x.BaseInventoryDeliveryItem.Value), ct))
                blockers.Add("یکی از خروج‌های انبار در رسید، برگشت یا سند انبار دیگری استفاده شده است.");
        }
        var accountedDeliveries = deliveries.Where(x => x.VoucherId.HasValue).ToArray();
        if (accountedDeliveries.Length > 0)
        {
            var ids = accountedDeliveries.Select(x => x.VoucherId!.Value).Distinct().ToArray();
            var numbers = await db.Vouchers.AsNoTracking().Where(x => ids.Contains(x.VoucherId))
                .Select(x => new { x.VoucherId, x.Number })
                .ToDictionaryAsync(x => x.VoucherId, x => x.Number, ct);
            foreach (var delivery in accountedDeliveries)
            {
                var number = numbers.TryGetValue(delivery.VoucherId!.Value, out int value) ? $" شماره {value}" : "";
                blockers.Add($"خروج انبار شماره {delivery.Number} (انبار {delivery.StockCode}) سند حسابداری جداگانه{number} دارد.");
            }
        }
        if (invoice.VoucherId != null && (voucher == null || voucher.State != TemporaryVoucher ||
                voucher.Type != SystemVoucher || voucher.IssuerSystem != SalesIssuer || voucher.IsMerged || voucher.Year != invoice.Year ||
                await db.SalesInvoices.AnyAsync(x => x.VoucherRef == voucher.Id && x.InvoiceId != id, ct) ||
                !await db.VoucherItems.AnyAsync(x => x.VoucherRef == voucher.Id, ct) ||
                await db.VoucherItems.AnyAsync(x => x.VoucherRef == voucher.Id &&
                    (x.IssuerEntityRef != id || x.IssuerEntityName == null ||
                        !(x.IssuerEntityName == InvoiceType || x.IssuerEntityName.StartsWith(InvoiceType + ","))), ct)))
            blockers.Add(voucher == null ? "سند حسابداری مرتبط با فاکتور پیدا نشد." :
                $"سند حسابداری شماره {voucher.Number} موقت و مختص این فاکتور نیست.");
        blockers.AddRange(await GetOtherBlockersAsync(db, id, ct));
        return new(invoice, items, deliveries, deliveryLines, voucher, blockers);
    }

    private static Task<List<string>> GetOtherBlockersAsync(SepidarContextDbBase db, int id, CancellationToken ct) =>
        db.Database.SqlQuery<string>($"""
            SELECT CASE
                WHEN MAX(CASE WHEN bill.State = {TaxPayerBillSent} THEN 1 ELSE 0 END) = 1
                    THEN N'این فاکتور به سامانهٔ مؤدیان ارسال شده و امکان حذف آن وجود ندارد.'
                WHEN MAX(CASE WHEN bill.State = {TaxPayerBillAwaitingInquiry} THEN 1 ELSE 0 END) = 1
                    THEN N'نتیجهٔ ارسال این فاکتور به سامانهٔ مؤدیان در انتظار استعلام است. تا مشخص شدن نتیجه، امکان حذف وجود ندارد.'
                ELSE N'این فاکتور سابقهٔ مرتبط با سامانهٔ مؤدیان دارد. برای حذف، ابتدا وضعیت سابقه را در سپیدار بررسی کنید.'
                END AS [Value]
            FROM GNR.TaxPayerBill bill
            WHERE bill.InvoiceRef = {id} OR bill.ExportServiceInvoiceRef = {id}
                OR EXISTS (SELECT 1 FROM GNR.TaxPayerBillItem line
                    JOIN SLS.InvoiceItem item ON item.InvoiceItemID = line.InvoiceItemRef
                    WHERE line.TaxPayerBillRef = bill.TaxPayerBillId AND item.InvoiceRef = {id})
            HAVING COUNT(*) > 0
            UNION ALL
            SELECT N'برای این فاکتور برگشت فروش ثبت شده است.'
            WHERE EXISTS(SELECT 1 FROM SLS.ReturnedInvoiceItem r JOIN SLS.InvoiceItem i
                ON i.InvoiceItemID=r.InvoiceItemRef OR i.InvoiceItemID=r.ForDiscountInvoiceItemRef WHERE i.InvoiceRef={id})
            UNION ALL
            SELECT N'برای این فاکتور اطلاعات دریافت یا تسویه ثبت شده است.'
            WHERE EXISTS(SELECT 1 FROM SLS.InvoiceReceiptInfo WHERE InvoiceRef={id})
                OR EXISTS(SELECT 1 FROM SLS.InvoiceReceiptChequeInfo WHERE InvoiceRef={id})
                OR EXISTS(SELECT 1 FROM SLS.InvoiceReceiptPosInfo WHERE InvoiceRef={id})
            """).ToListAsync(ct);

    private static async Task DeleteDestinationAsync(SepidarContextDbBase db, DeletionPlan plan, CancellationToken ct)
    {
        var invoice = plan.Invoice;
        var stockByDelivery = plan.Deliveries.ToDictionary(x => x.Id, x => x.StockRef);
        var restored = plan.DeliveryLines.GroupBy(x => (StockRef: stockByDelivery[x.DeliveryId], x.ItemRef, x.TracingRef))
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));
        var keys = plan.Items.Select(x => (x.StockRef, x.ItemRef, x.TracingRef)).Concat(restored.Keys).Distinct().ToArray();
        using var stockTable = SepidarStockSummary.CreateTable(keys, invoice.Year);
        var stockBefore = new Dictionary<(int, int, int?), decimal>();
        if (keys.Length > 0)
        {
            await SepidarStockSummary.LockAsync(db, stockTable, ct);
            await SepidarStockSummary.RecalculateAsync(db, stockTable, ct);
            if (restored.Count > 0)
                stockBefore = await ReadStockAsync(db, invoice.Year, restored.Keys, ct);
        }
        await UpdateQuotationAsync(db, plan.Items, invoice.Id, ct);

        await DeleteSalesDeliveriesAsync(db, plan.Deliveries, ct);
        await DeleteSupplementaryDataAsync(db, InvoiceType, invoice.Id, ct);
        await db.SalesInvoices.Where(x => x.InvoiceId == invoice.Id).ExecuteDeleteAsync(ct);
        if (plan.Voucher is { } voucher)
        {
            await DeleteSupplementaryDataAsync(db, VoucherType, voucher.Id, ct);
            await db.Vouchers.Where(x => x.VoucherId == voucher.Id).ExecuteDeleteAsync(ct);
        }
        if (keys.Length > 0)
            await SepidarStockSummary.RecalculateAsync(db, stockTable, ct);
        if (restored.Count > 0)
        {
            var after = await ReadStockAsync(db, invoice.Year, restored.Keys, ct);
            if (restored.Any(x => after.GetValueOrDefault(x.Key) != stockBefore.GetValueOrDefault(x.Key) + x.Value))
                throw new SalesInvoiceDestinationException("موجودی سپیدار به مقدار مورد انتظار برنگشت. فاکتور و اسنادش حذف نشدند.");
        }
    }

    private static async Task DeleteSalesDeliveriesAsync(SepidarContextDbBase db, List<DeliveryHeader> deliveries, CancellationToken ct)
    {
        foreach (var delivery in deliveries)
            await DeleteSupplementaryDataAsync(db, DeliveryType, delivery.Id, ct);
        var deliveryIds = deliveries.Select(x => x.Id).ToArray();
        if (deliveryIds.Length > 0)
            await db.InventoryDeliveries.Where(x => deliveryIds.Contains(x.InventoryDeliveryID)).ExecuteDeleteAsync(ct);
    }

    private static async Task UpdateQuotationAsync(SepidarContextDbBase db, List<InvoiceLine> lines, int? invoiceId, CancellationToken ct)
    {
        var ids = lines.Where(x => x.QuotationItemId.HasValue).Select(x => x.QuotationItemId!.Value).Distinct().ToArray();
        if (ids.Length == 0)
            return;
        var quotes = await db.QuotationItems.Where(x => ids.Contains(x.QuotationItemID)).ToListAsync(ct);
        var remaining = await (from row in db.SalesInvoiceItems.AsNoTracking()
            join invoice in db.SalesInvoices.AsNoTracking() on row.InvoiceRef equals invoice.InvoiceId
            where row.QuotationItemRef.HasValue && ids.Contains(row.QuotationItemRef.Value) &&
                (!invoiceId.HasValue || invoice.InvoiceId != invoiceId.Value) && invoice.State == ActiveInvoice
            group row by new { QuotationItemId = row.QuotationItemRef!.Value, row.ItemRef } into groupRows
            select new { groupRows.Key, Quantity = groupRows.Sum(row => row.Quantity) }).ToListAsync(ct);
        var remainingByItem = remaining.ToDictionary(x => (x.Key.QuotationItemId, x.Key.ItemRef), x => x.Quantity);
        foreach (var quote in quotes)
        {
            if (quote.UsedQuantity is > 0)
                quote.UsedQuantity = Math.Min(quote.UsedQuantity.Value, Math.Max(0,
                    remainingByItem.GetValueOrDefault((quote.QuotationItemID, quote.ItemRef))));
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task DeleteSupplementaryDataAsync(SepidarContextDbBase db, string type, int id, CancellationToken ct)
    {
        await db.ExtraData.Where(x => x.EntityRef == id && x.EntityTypeName == type).ExecuteDeleteAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM FMK.Note WHERE EntityRef={id} AND EntityType={type}", ct);
    }

    private static async Task<Dictionary<(int, int, int?), decimal>> ReadStockAsync(SepidarContextDbBase db, int year,
        IEnumerable<(int StockRef, int ItemRef, int? TracingRef)> keys, CancellationToken ct)
    {
        var stocks = keys.Select(x => x.StockRef).Distinct().ToArray();
        var items = keys.Select(x => x.ItemRef).Distinct().ToArray();
        var balances = await db.ItemStockSummaries.AsNoTracking().Where(x => x.FiscalYearRef == year &&
            stocks.Contains(x.StockRef) && items.Contains(x.ItemRef) && x.UnitRef == x.Item.UnitRef)
            .GroupBy(x => new { x.StockRef, x.ItemRef, x.TracingRef })
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) }).ToListAsync(ct);
        return balances.ToDictionary(x => (x.Key.StockRef, x.Key.ItemRef, x.Key.TracingRef), x => x.Quantity);
    }

    private sealed record InvoiceHeader(int Id, int Number, int Version, DateTime Modified, int Year, int State,
        bool BaseOnDelivery, int? OrderId, int? VoucherId);
    private sealed record InvoiceLine(int Id, int ItemRef, int StockRef, int? TracingRef, decimal Quantity,
        int? QuotationItemId, int? OrderItemId);
    private sealed record DeliveryHeader(int Id, int Number, int StockRef, int StockCode, int Year, int Type, int CreatorForm,
        bool IsReturn, int? VoucherId, int Version, DateTime Modified);
    private sealed record DeliveryLine(int Id, int DeliveryId, int? InvoiceItemId, int ItemRef, int? TracingRef,
        decimal Quantity, int Version, bool IsReturn, int? BaseDeliveryItemId);
    private sealed record VoucherHeader(int Id, int Number, int Year, int State, int Type, int IssuerSystem,
        bool IsMerged, int Version, DateTime Modified);
    private sealed record DeletionPlan(InvoiceHeader Invoice, List<InvoiceLine> Items, List<DeliveryHeader> Deliveries,
        List<DeliveryLine> DeliveryLines, VoucherHeader? Voucher, List<string> Blockers)
    {
        public string Token => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { Invoice, Items, Deliveries, DeliveryLines, Voucher })));
    }
}
