using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;

public sealed partial class SupplyDestinationMaintenanceRepository
{
    public async Task<SupplyReceiptEditState> GetReceiptEditAsync(SupplyDestinationDocument document, CancellationToken ct)
    {
        var db = Resolve(document);
        var plan = await BuildPlanAsync(db, document, false, ct);
        var receipt = plan.Receipt ?? throw new InvalidOperationException("رسید انبار معتبر نیست.");
        var stock = await db.Stocks.AsNoTracking().Where(x => x.StockID == receipt.StockRef)
            .Select(x => x.Title).SingleOrDefaultAsync(ct);
        return new(new(receipt.Number, plan.Voucher?.Number, plan.Token, plan.Blockers), receipt.Date, receipt.StockRef,
            stock, receipt.Description, plan.ReceiptRows.Select(row =>
                new SupplyReceiptEditRow(row.InventoryReceiptItemID, row.ItemRef, row.Quantity, row.Description)).ToArray());
    }

    public async Task EditReceiptAsync(SupplyDestinationDocument document, string token, int modifier, SupplyReceiptEdit edit,
        Func<SupplyReceiptPosting, CancellationToken, Task<int?>> postVoucher, CancellationToken ct)
    {
        var db = Resolve(document);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await BuildPlanAsync(db, document, true, ct);
        ValidatePlan(plan, token, deleting: false);
        var receipt = plan.Receipt ?? throw new InvalidOperationException("رسید انبار معتبر نیست.");
        var oldStock = receipt.StockRef;
        var stockChanged = edit.StockId != oldStock;
        var year = receipt.FiscalYearRef;
        var date = edit.Date.Date;
        if (date < plan.Purchase.Date.Date || !await db.FiscalYears.AnyAsync(x => x.FiscalYearId == year &&
                x.StartDate <= date && x.EndDate >= date, ct))
            throw new InvalidOperationException("تاریخ رسید باید در همان سال مالی و در تاریخ فاکتور خرید یا پس از آن باشد.");
        if (edit.Description?.Length > 4000 || edit.RowDescriptions == null ||
            edit.RowDescriptions.Count != plan.ReceiptRows.Count ||
            plan.ReceiptRows.Any(row => !edit.RowDescriptions.ContainsKey(row.InventoryReceiptItemID)) ||
            edit.RowDescriptions.Values.Any(value => value?.Length > 4000))
            throw new InvalidOperationException("توضیحات رسید یا اقلام آن معتبر نیست.");

        var items = plan.ReceiptRows.Select(x => x.ItemRef).Distinct().ToArray();
        if (stockChanged && await db.ItemStocks.Where(x => x.StockRef == edit.StockId && x.Stock.IsActive &&
                items.Contains(x.ItemRef)).Select(x => x.ItemRef).Distinct().CountAsync(ct) != items.Length)
            throw new InvalidOperationException("انبار انتخاب‌شده برای همه اقلام رسید معتبر نیست.");
        if ((stockChanged || date != receipt.Date.Date) && await db.Database.SqlQuery<bool>($"""
            SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM INV.InventoryPricing
                WHERE StockRef={edit.StockId} AND FiscalYearRef={year} AND EndDate >= {date})
                THEN 1 ELSE 0 END AS bit) AS [Value]
            """).SingleAsync(ct))
            throw new InvalidOperationException("انبار مقصد برای این تاریخ قیمت‌گذاری شده است.");

        var quantities = plan.ReceiptRows.GroupBy(x => (x.ItemRef, x.TracingRef))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        using var summary = SepidarStockSummary.CreateTable(
            new[] { oldStock, edit.StockId }.Distinct().SelectMany(stock => quantities.Keys.Select(key =>
                (stock, key.ItemRef, key.TracingRef))), year);
        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var oldBefore = await ReadStockAsync(db, year, oldStock, items, ct);
        var newBefore = stockChanged ? await ReadStockAsync(db, year, edit.StockId, items, ct) : oldBefore;

        db.Attach(receipt);
        receipt.Date = date;
        receipt.StockRef = edit.StockId;
        receipt.Description = edit.Description;
        receipt.LastModifier = modifier;
        receipt.LastModificationDate = DateTime.Now;
        receipt.Version++;
        foreach (var row in plan.ReceiptRows)
        {
            var description = edit.RowDescriptions[row.InventoryReceiptItemID];
            if (description == row.Description)
                continue;
            db.Attach(row);
            row.Description = description;
            row.Version++;
        }

        // The posting routine may save the tracked receipt. Keep the old voucher until the new FK is saved.
        receipt.AccountingVoucherRef = await postVoucher(new(receipt, plan.Purchase.InvoiceNumber, stockChanged), ct);
        await db.SaveChangesAsync(ct);
        if (plan.Voucher is { } voucher)
        {
            await DeleteSupplementaryDataAsync(db, VoucherType, voucher.VoucherId, ct);
            await db.Vouchers.Where(x => x.VoucherId == voucher.VoucherId).ExecuteDeleteAsync(ct);
        }

        await SepidarStockSummary.LockAsync(db, summary, ct);
        await SepidarStockSummary.RecalculateAsync(db, summary, ct);
        var oldAfter = await ReadStockAsync(db, year, oldStock, items, ct);
        var newAfter = stockChanged ? await ReadStockAsync(db, year, edit.StockId, items, ct) : oldAfter;
        if (quantities.Any(x => stockChanged
                ? oldAfter.GetValueOrDefault(x.Key) < 0m ||
                  oldBefore.GetValueOrDefault(x.Key) - oldAfter.GetValueOrDefault(x.Key) != x.Value ||
                  newAfter.GetValueOrDefault(x.Key) - newBefore.GetValueOrDefault(x.Key) != x.Value
                : oldBefore.GetValueOrDefault(x.Key) != oldAfter.GetValueOrDefault(x.Key)))
            throw new InvalidOperationException("ویرایش رسید باعث مغایرت موجودی انبار می‌شود. تغییرات اعمال نشد.");
        await transaction.CommitAsync(ct);
    }
}
