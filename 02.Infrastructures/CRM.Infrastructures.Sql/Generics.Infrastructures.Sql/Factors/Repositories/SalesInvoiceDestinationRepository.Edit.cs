using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Repositories;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Factors.Repositories;

public sealed partial class SalesInvoiceDestinationRepository
{
    public async Task<SalesInvoiceEditState> GetEditAsync(SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        var (db, mapping, id) = await ResolveAsync(key, ct);
        var snapshot = await GetEditSnapshotAsync(db, id, false, ct);
        var invoice = snapshot.Invoice;
        var itemIds = snapshot.Rows.Select(x => x.ItemRef).Distinct().ToArray();
        var stockIds = snapshot.Rows.Select(x => x.StockRef).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.ItemID))
            .ToDictionaryAsync(x => x.ItemID, x => x.Code + " - " + x.Title, ct);
        var stocks = await db.Stocks.AsNoTracking().Where(x => stockIds.Contains(x.StockID))
            .Select(x => new { x.StockID, x.Code, x.Title }).ToDictionaryAsync(x => x.StockID, x => $"{x.Code} - {x.Title}", ct);
        var impact = new SalesInvoiceDeletionPreview(invoice.Number, mapping.ExternalSoftware.Title, snapshot.Token,
            snapshot.Plan.Voucher == null ? [] : [snapshot.Plan.Voucher.Number],
            snapshot.Plan.Deliveries.Select(x => new SalesInvoiceDeliveryNumber(x.Number, x.StockCode)).ToArray(), snapshot.Plan.Blockers);
        return new(impact, invoice.Date, invoice.QuotationRef, invoice.CustomerPartyRef, invoice.CustomerRealName,
            invoice.SaleTypeRef, snapshot.Broker?.PartyRef, snapshot.Rows.Select(row => new SalesInvoiceEditRow(
                row.InvoiceItemID, row.RowID, row.ItemRef, items.GetValueOrDefault(row.ItemRef), row.StockRef,
                stocks.GetValueOrDefault(row.StockRef), row.Quantity, row.QuotationItemRef.HasValue)).ToArray());
    }

    public async Task EditAsync(SalesInvoiceDestinationKey key, string token, int modifier, SalesInvoiceEdit edit,
        Func<CancellationToken, Task> lockPosting, Func<SalesInvoicePosting, CancellationToken, Task> post, CancellationToken ct)
    {
        await using var mappingTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (db, _, id) = await ResolveAsync(key, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await lockPosting(ct);
        var snapshot = await GetEditSnapshotAsync(db, id, true, ct);
        var plan = snapshot.Plan;
        if (plan.Blockers.Count > 0)
            throw new SalesInvoiceDestinationException(string.Join(" ", plan.Blockers));
        if (snapshot.Token != token)
            throw new SalesInvoiceDestinationException("فاکتور یا اسناد وابسته تغییر کرده‌اند. فرم را دوباره باز کنید.");

        var invoice = snapshot.Invoice;
        var date = edit.Date.Date;
        await ValidateEditHeaderAsync(db, invoice, edit, date, ct);
        var selections = edit.Items.ToDictionary(x => x.Id);
        if (selections.Count != snapshot.Rows.Count || snapshot.Rows.Any(row => !selections.ContainsKey(row.InvoiceItemID)))
            throw new SalesInvoiceDestinationException("ردیف‌های ویرایش با فاکتور مقصد مطابقت ندارند.");
        var changedRows = snapshot.Rows.Where(row =>
            selections[row.InvoiceItemID].ItemId != row.ItemRef || selections[row.InvoiceItemID].StockId != row.StockRef).ToArray();
        await ValidateEditItemsAsync(db, changedRows, selections, ct);
        var nextStocks = selections.Values.Select(x => x.StockId).Distinct().ToArray();
        if (edit.AutoDelivery && await HasPricedStockAsync(db, invoice.FiscalYearRef, date, nextStocks, ct))
            throw new SalesInvoiceDestinationException("انبار انتخاب‌شده برای تاریخ خروج جدید قیمت‌گذاری شده است.");

        var stockByDelivery = plan.Deliveries.ToDictionary(x => x.Id, x => x.StockRef);
        var restored = plan.DeliveryLines.GroupBy(row => (stockByDelivery[row.DeliveryId], row.ItemRef, row.TracingRef))
            .ToDictionary(g => g.Key, g => g.Sum(row => row.Quantity));
        var issued = snapshot.Rows.GroupBy(row => {
            var selected = selections[row.InvoiceItemID];
            return (selected.StockId, selected.ItemId, row.TracingRef);
        }).ToDictionary(g => g.Key, g => g.Sum(row => row.Quantity));
        var keys = plan.Items.Select(row => (row.StockRef, row.ItemRef, row.TracingRef)).Concat(restored.Keys)
            .Concat(issued.Keys).Distinct().ToArray();
        using var summary = SepidarStockSummary.CreateTable(keys, invoice.FiscalYearRef);
        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var before = await ReadStockAsync(db, invoice.FiscalYearRef, keys, ct);

        try
        {
            await DeleteSalesDeliveriesAsync(db, plan.Deliveries, ct);
            await db.InvoiceBrokers.Where(x => x.InvoiceRef == id).ExecuteDeleteAsync(ct);
            invoice.InvoiceItems = snapshot.Rows;
            db.Attach(invoice);
            foreach (var row in changedRows)
            {
                var selected = selections[row.InvoiceItemID];
                row.ItemRef = selected.ItemId;
                row.StockRef = selected.StockId;
                row.QuotationItemRef = null;
            }
            if (edit.CustomerId is int customerId && customerId != invoice.CustomerPartyRef)
            {
                var customer = await db.Parties.AsNoTracking()
                    .Where(x => x.PartyId == customerId && x.IsCustomer && x.Dl != null && x.Dl.IsActive)
                    .Select(x => new { x.PartyId, Name = (x.Name + " " + x.LastName).Trim(),
                        PartyMainAddressRef = x.PartyAddresses.Where(p => p.IsMain == 1).Select(p => (int?)p.PartyAddressId).FirstOrDefault() })
                    .SingleOrDefaultAsync(ct)
                    ?? throw new SalesInvoiceDestinationException("مشتری مقصد معتبر نیست.");
                invoice.CustomerPartyRef = customer.PartyId;
                invoice.CustomerRealName = invoice.CustomerRealName_En = customer.Name;
                invoice.PartyAddressRef = customer.PartyMainAddressRef;
            }
            invoice.SaleTypeRef = edit.SaleTypeId ?? invoice.SaleTypeRef;
            if (invoice.Date.Date != date)
                invoice.TaxPayerBillIssueDateTime = date;
            invoice.Date = date;
            invoice.Number = edit.Number;
            invoice.VoucherRef = null;
            invoice.Version++;
            invoice.LastModifier = modifier;
            invoice.LastModificationDate = DateTime.Now;

            // The stock summary ignores state 2. Exclude this invoice's own demand until the new stock is validated.
            invoice.State = 2;
            await db.SaveChangesAsync(ct);
            await SepidarStockSummary.LockAsync(db, summary, ct);
            await SepidarStockSummary.RecalculateAsync(db, summary, ct);

            var numbers = plan.Deliveries.GroupBy(x => x.StockRef).ToDictionary(x => x.Key, x => x.Max(row => row.Number));
            await post(new(invoice, edit.BrokerPartyId, numbers), ct);
            if (invoice.State != ActiveInvoice || invoice.VoucherRef.HasValue != edit.AutoVoucher)
                throw new SalesInvoiceDestinationException("بازسازی اسناد وابسته فاکتور تکمیل نشد.");
            if (plan.Voucher is { } previousVoucher)
            {
                await DeleteSupplementaryDataAsync(db, VoucherType, previousVoucher.Id, ct);
                await db.Vouchers.Where(x => x.VoucherId == previousVoucher.Id).ExecuteDeleteAsync(ct);
            }
            var changedIds = changedRows.Select(row => row.InvoiceItemID).ToHashSet();
            await UpdateQuotationAsync(db, plan.Items.Where(row => changedIds.Contains(row.Id)).ToList(), null, ct);
            await SepidarStockSummary.LockAsync(db, summary, ct);
            await SepidarStockSummary.RecalculateAsync(db, summary, ct);
            var after = await ReadStockAsync(db, invoice.FiscalYearRef, keys, ct);
            if (keys.Any(key => after.GetValueOrDefault(key) != before.GetValueOrDefault(key) + restored.GetValueOrDefault(key) -
                    (edit.AutoDelivery ? issued.GetValueOrDefault(key) : 0m)))
                throw new SalesInvoiceDestinationException("بازسازی خروج‌ها با تغییر مورد انتظار موجودی تطبیق ندارد. ویرایش انجام نشد.");
            await transaction.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new SalesInvoiceDestinationException("سند وابسته‌ای در سپیدار مانع ویرایش است. تغییرات اعمال نشد.");
        }
    }

    private static async Task ValidateEditHeaderAsync(SepidarContextDbBase db, SalesInvoiceRecord invoice,
        SalesInvoiceEdit edit, DateTime date, CancellationToken ct)
    {
        if (!await db.FiscalYears.AnyAsync(x => x.FiscalYearId == invoice.FiscalYearRef &&
                x.StartDate <= date && x.EndDate >= date, ct))
            throw new SalesInvoiceDestinationException("تاریخ فاکتور باید در همان سال مالی سند باشد.");
        if (invoice.QuotationRef is int quotationId)
        {
            if (edit.CustomerId.HasValue || edit.SaleTypeId.HasValue)
                throw new SalesInvoiceDestinationException("مشتری و نوع فروش از پیش‌فاکتور سپیدار می‌آیند.");
            var quotationDate = await db.Quotations.AsNoTracking().Where(x => x.QuotationId == quotationId)
                .Select(x => (DateTime?)x.Date).SingleOrDefaultAsync(ct);
            if (!quotationDate.HasValue || date < quotationDate.Value.Date)
                throw new SalesInvoiceDestinationException("تاریخ فاکتور نمی‌تواند قبل از تاریخ پیش‌فاکتور باشد.");
        }
        else if (edit.CustomerId is not > 0 || edit.SaleTypeId is not > 0 ||
            !await db.SaleTypes.AnyAsync(x => x.SaleTypeId == edit.SaleTypeId, ct))
            throw new SalesInvoiceDestinationException("مشتری و نوع فروش مقصد معتبر نیست.");
        if (edit.Number <= 0 || await db.SalesInvoices.AnyAsync(x => x.InvoiceId != invoice.InvoiceId &&
                x.FiscalYearRef == invoice.FiscalYearRef && x.Number == edit.Number, ct))
            throw new SalesInvoiceDestinationException("شماره فاکتور در سال مالی مقصد معتبر نیست یا قبلاً استفاده شده است.");
    }

    private static async Task ValidateEditItemsAsync(SepidarContextDbBase db, SalesInvoiceItemRecord[] changed,
        IReadOnlyDictionary<int, SalesInvoiceEditSelection> selections, CancellationToken ct)
    {
        if (changed.Length == 0)
            return;
        var selectedRows = changed.Select(x => selections[x.InvoiceItemID]).ToArray();
        var targetItemIds = selectedRows.Select(x => x.ItemId).Distinct().ToArray();
        var targetStockIds = selectedRows.Select(x => x.StockId).Distinct().ToArray();
        var itemIds = targetItemIds.Concat(changed.Select(x => x.ItemRef)).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.ItemID))
            .ToDictionaryAsync(x => x.ItemID, ct);
        var allowedStocks = await db.ItemStocks.AsNoTracking().Where(x => targetItemIds.Contains(x.ItemRef) &&
                targetStockIds.Contains(x.StockRef) && x.Stock.IsActive)
            .Select(x => new { x.ItemRef, x.StockRef }).ToListAsync(ct);
        var pairs = allowedStocks.Select(x => (x.ItemRef, x.StockRef)).ToHashSet();
        foreach (var row in changed)
        {
            var selected = selections[row.InvoiceItemID];
            if (row.TracingRef.HasValue || row.SecondaryQuantity.HasValue || row.ProductPackRef.HasValue ||
                !items.TryGetValue(selected.ItemId, out var item) || !item.IsActive || !item.Sellable ||
                item.SerialTracking || item.IsPricingBasedOnTracing || item.SecondaryUnitRef.HasValue ||
                items.GetValueOrDefault(row.ItemRef)?.UnitRef != item.UnitRef ||
                !pairs.Contains((selected.ItemId, selected.StockId)))
                throw new SalesInvoiceDestinationException($"کالای معادل یا انبار ردیف {row.RowID} معتبر نیست.");
        }
    }

    private static async Task<EditSnapshot> GetEditSnapshotAsync(SepidarContextDbBase db, int id, bool forUpdate, CancellationToken ct)
    {
        var plan = await BuildPlanAsync(db, id, forUpdate, ct)
            ?? throw new SalesInvoiceDestinationException("فاکتور در سپیدار پیدا نشد.");
        var invoice = await db.SalesInvoices.AsNoTracking().SingleAsync(x => x.InvoiceId == id, ct);
        var rows = await db.SalesInvoiceItems.AsNoTracking().Where(x => x.InvoiceRef == id)
            .OrderBy(x => x.RowID).ThenBy(x => x.InvoiceItemID).ToListAsync(ct);
        var brokers = await db.InvoiceBrokers.AsNoTracking().Where(x => x.InvoiceRef == id)
            .OrderBy(x => x.BrokerID).ToListAsync(ct);
        if (rows.Count == 0 || rows.Any(x => x.Quantity <= 0) || brokers.Count > 1)
            plan.Blockers.Add("اقلام یا واسط فاکتور برای ویرایش از این صفحه معتبر نیست.");
        if (plan.Deliveries.Count > 0)
        {
            var delivered = plan.DeliveryLines.Where(x => x.InvoiceItemId.HasValue).GroupBy(x => x.InvoiceItemId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
            if (rows.Any(row => delivered.GetValueOrDefault(row.InvoiceItemID) != row.Quantity))
                plan.Blockers.Add("خروج‌های فعلی همه اقلام فاکتور را پوشش نمی‌دهند.");
            if (await HasPricedStockAsync(db, invoice.FiscalYearRef, invoice.Date, plan.Deliveries.Select(x => x.StockRef).Distinct().ToArray(), ct))
                plan.Blockers.Add("خروج انبار فاکتور در دورهٔ قیمت‌گذاری‌شده قرار دارد.");
        }
        return new(plan, invoice, rows, brokers.FirstOrDefault());
    }

    private static Task<bool> HasPricedStockAsync(SepidarContextDbBase db, int year, DateTime date, int[] stocks, CancellationToken ct)
    {
        if (stocks.Length == 0)
            return Task.FromResult(false);
        return db.Database.SqlQuery<int>($"""
            SELECT StockRef AS [Value] FROM INV.InventoryPricing WHERE FiscalYearRef={year} AND EndDate >= {date}
            """).AnyAsync(stock => stocks.Contains(stock), ct);
    }

    private sealed record EditSnapshot(DeletionPlan Plan, SalesInvoiceRecord Invoice,
        List<SalesInvoiceItemRecord> Rows, InvoiceBrokerRecord? Broker)
    {
        public string Token => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { Plan.Token, Invoice, Rows, Broker })));
    }
}
