using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Models;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Factors.Repositories;

public sealed partial class SalesInvoiceDestinationRepository
{
    private const string SynchronizeFirst = "ابتدا فاکتور را با ERP همگام‌سازی کنید.";

    public async Task<bool> HasSyncDifferenceAsync(SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        var (db, _, id) = await ResolveAsync(key, ct);
        var invoice = await db.SalesInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.InvoiceId == id, ct);
        if (invoice == null) return false;
        var source = await ReadSyncSourceAsync(key.SourceId, ct);
        var amounts = await db.SalesInvoiceItems.AsNoTracking().Where(x => x.InvoiceRef == id)
            .OrderBy(x => x.RowID).ThenBy(x => x.InvoiceItemID)
            .Select(x => new SyncLineAmounts(x.Quantity, x.Fee, x.Price, x.Discount, x.Tax, x.NetPriceInBaseCurrency))
            .ToListAsync(ct);
        return HasSyncDifference(source, invoice, amounts);
    }

    public async Task<SalesInvoiceSyncPreview> GetSyncPreviewAsync(SalesInvoiceDestinationKey key, CancellationToken ct)
    {
        var (db, mapping, id) = await ResolveAsync(key, ct);
        var snapshot = await GetEditSnapshotAsync(db, id, false, ct);
        var source = await ReadSyncSourceAsync(key.SourceId, ct);
        var presentation = await ReadSyncRowsAsync(db, key, source, snapshot, ct);
        var blockers = await GetSyncBlockersAsync(db, source, snapshot, ct);
        var impact = new SalesInvoiceDeletionPreview(snapshot.Invoice.Number, mapping.ExternalSoftware.Title,
            GetSyncToken(snapshot, source, presentation.SourceRows),
            snapshot.Plan.Voucher is { } voucher ? [voucher.Number] : [],
            snapshot.Plan.Deliveries.Select(x => new SalesInvoiceDeliveryNumber(x.Number, x.StockCode)).ToArray(), blockers);
        return new(impact, presentation.SourceRows, presentation.DestinationRows, SourceAmounts(source),
            DestinationAmounts(snapshot.Invoice), snapshot.Invoice.QuotationRef.HasValue);
    }

    public async Task SynchronizeAsync(SalesInvoiceSyncRequest request, int modifier,
        Func<CancellationToken, Task> lockPosting,
        Func<int, List<SepidarInvoiceExportItemPreview>, decimal, CancellationToken, Task<List<SalesInvoiceItemRecord>>> createRows,
        Func<SalesInvoicePosting, CancellationToken, Task> post, CancellationToken ct)
    {
        await using var sourceTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (db, _, id) = await ResolveAsync(request.Key, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await lockPosting(ct);
        var snapshot = await GetEditSnapshotAsync(db, id, true, ct);
        var source = await ReadSyncSourceAsync(request.Key.SourceId, ct);
        var rows = (await ReadSyncRowsAsync(db, request.Key, source, snapshot, ct)).SourceRows;
        if (GetSyncToken(snapshot, source, rows) != request.Token)
            throw new SalesInvoiceDestinationException("اقلام یا اسناد وابسته تغییر کرده‌اند. پیش‌نمایش را دوباره باز کنید.");
        var blockers = await GetSyncBlockersAsync(db, source, snapshot, ct);
        if (blockers.Count > 0)
            throw new SalesInvoiceDestinationException(blockers[0]);

        var lines = await PrepareSyncLinesAsync(db, rows, request.Items, ct);
        var invoice = snapshot.Invoice;
        if (request.AutoDelivery && await HasPricedStockAsync(db, invoice.FiscalYearRef, invoice.Date,
                lines.Select(x => x.StockRef).Distinct().ToArray(), ct))
            throw new SalesInvoiceDestinationException("یکی از انبارهای انتخاب‌شده برای تاریخ فاکتور قیمت‌گذاری شده است.");
        var issued = lines.GroupBy(x => (x.StockRef, x.ItemRef, (int?)null))
            .ToDictionary(x => x.Key, x => x.Sum(row => row.Quantity));
        await RebuildEditedInvoiceAsync(db, snapshot, modifier, issued, request.AutoVoucher, request.AutoDelivery,
            snapshot.Broker?.PartyRef,
            async () =>
            {
                if (await HasSyncRowReferencesAsync(db, id, ct))
                    throw new SalesInvoiceDestinationException("یکی از اقلام فاکتور در سند دیگری در سپیدار استفاده شده است.");
                var replacement = await createRows(id, lines, invoice.Rate, ct);
                await db.SalesInvoiceItems.Where(x => x.InvoiceRef == id).ExecuteDeleteAsync(ct);
                db.Attach(invoice);
                invoice.InvoiceItems = replacement;
                db.SalesInvoiceItems.AddRange(replacement);
                invoice.Price = invoice.PriceInBaseCurrency = source.SumTotalAmount;
                invoice.Discount = invoice.DiscountInBaseCurrency = source.Discount;
                invoice.Tax = invoice.TaxInBaseCurrency = source.TotalTax;
                invoice.NetPriceInBaseCurrency = source.PaymentAmount;
            }, () => ReconcileSyncQuotationAsync(db, snapshot.Rows, lines, ct), post, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<Factor> ReadSyncSourceAsync(Guid sourceId, CancellationToken ct) =>
        await crm.Factors.AsNoTracking().Where(x => x.Id == sourceId && !x.IsDeleted)
            .Select(x => new Factor {
                Id = x.Id, PfactorID = x.PfactorID, CurrencyType = x.CurrencyType, ParentReturnedId = x.ParentReturnedId,
                Returned = x.Returned, SumTotalAmount = x.SumTotalAmount, Discount = x.Discount, TotalTax = x.TotalTax,
                PaymentAmount = x.PaymentAmount, FactorItems = x.FactorItems.Where(row => !row.IsDeleted)
                    .OrderBy(row => row.Row).ThenBy(row => row.Id).ToList()
            }).SingleOrDefaultAsync(ct)
        ?? throw new SalesInvoiceDestinationException("فاکتور فعال در ERP پیدا نشد.");

    private async Task<SyncPresentation> ReadSyncRowsAsync(SepidarContextDbBase db, SalesInvoiceDestinationKey key,
        Factor source, EditSnapshot snapshot, CancellationToken ct)
    {
        var quotationRows = new Dictionary<Guid, QuotationItem>();
        if (snapshot.Invoice.QuotationRef is int quoteId)
        {
            var quoteExternalId = quoteId.ToString(CultureInfo.InvariantCulture);
            var mapped = await crm.ExternalMappings.AsNoTracking().Where(x => x.ExternalSoftwareId == key.ExternalSoftwareId &&
                x.ExEntityName == nameof(Quotation) && x.ExEntityId == quoteExternalId)
                .Select(x => new { x.EntityId, x.MapType, x.EntityName }).Take(2).ToListAsync(ct);
            if (mapped.Count != 1 || mapped[0].EntityId != source.PfactorID ||
                mapped[0].MapType != ExternalMappingTypeEnum.پیش_فاکتور || mapped[0].EntityName != nameof(PreFactor))
                throw new SalesInvoiceDestinationException("ارتباط پیش‌فاکتور ERP با پیش‌فاکتور این فاکتور در سپیدار معتبر نیست.");
            var ids = source.FactorItems.Select(row => row.PfItemId).OfType<Guid>().Distinct().ToArray();
            var crmRows = await crm.PreFactorItems.AsNoTracking().Where(x => x.PrefactorId == source.PfactorID &&
                !x.IsDeleted && ids.Contains(x.Id)).Select(x => new { x.Id, x.Row }).ToListAsync(ct);
            var quoteRows = (await db.QuotationItems.AsNoTracking().Where(x => x.QuotationRef == quoteId).ToListAsync(ct))
                .GroupBy(x => x.RowID).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.Single());
            foreach (var row in crmRows)
                if (quoteRows.TryGetValue(row.Row, out var target))
                    quotationRows[row.Id] = target;
        }

        var itemIds = snapshot.Rows.Select(x => x.ItemRef).Concat(quotationRows.Values.Select(x => x.ItemRef)).Distinct().ToArray();
        var stockIds = snapshot.Rows.Select(x => x.StockRef).Concat(quotationRows.Values.Select(x => x.StockRef)).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.ItemID))
            .Select(x => new { x.ItemID, x.Code, x.Title }).ToDictionaryAsync(x => x.ItemID, ct);
        var stocks = await db.Stocks.AsNoTracking().Where(x => stockIds.Contains(x.StockID))
            .Select(x => new { x.StockID, x.Code, x.Title }).ToDictionaryAsync(x => x.StockID, x => $"{x.Code} - {x.Title}", ct);
        string? ItemTitle(int id) => items.TryGetValue(id, out var item) ? $"{item.Code} - {item.Title}" : null;
        var oldPairs = snapshot.Rows.Where(x => items.ContainsKey(x.ItemRef)).GroupBy(x => NormalizeSyncTitle(items[x.ItemRef].Title))
            .ToDictionary(g => g.Key, g => g.Select(x => (x.ItemRef, x.StockRef)).Distinct().Take(2).ToArray());
        var rows = source.FactorItems.Select(row =>
        {
            var quote = row.PfItemId is Guid pfRow ? quotationRows.GetValueOrDefault(pfRow) : null;
            var choices = oldPairs.GetValueOrDefault(NormalizeSyncTitle(row.Product));
            var suggestion = quote != null ? (quote.ItemRef, quote.StockRef) : choices?.Length == 1 ? choices[0] : (0, 0);
            var amounts = SourceLine(row);
            return new SalesInvoiceSyncRow(row.Id, $"{row.ProductSmallName} {row.Product}".Trim(), row.UnitTxt,
                amounts.Quantity, amounts.Fee, amounts.Price, amounts.Discount, amounts.Tax,
                suggestion.Item1, suggestion.Item2, quote?.QuotationItemID, ItemTitle(suggestion.Item1),
                stocks.GetValueOrDefault(suggestion.Item2));
        }).ToArray();
        var current = snapshot.Rows.Select(row => new SalesInvoiceSyncDestinationRow(ItemTitle(row.ItemRef),
            stocks.GetValueOrDefault(row.StockRef), row.Quantity, row.Fee, row.Price, row.Discount, row.Tax)).ToArray();
        return new(rows, current);
    }

    private static async Task<List<SepidarInvoiceExportItemPreview>> PrepareSyncLinesAsync(SepidarContextDbBase db,
        IReadOnlyList<SalesInvoiceSyncRow> rows, SalesInvoiceSyncSelection[] selections, CancellationToken ct)
    {
        if (selections == null || selections.Length != rows.Count || selections.Any(x => x == null || x.ItemId <= 0 || x.StockId <= 0)
            || selections.Select(x => x.SourceRowId).Distinct().Count() != rows.Count)
            throw new SalesInvoiceDestinationException("برای هر قلم فاکتور، کالا و انبار معتبر انتخاب کنید.");
        var byId = selections.ToDictionary(x => x.SourceRowId);
        var itemIds = selections.Select(x => x.ItemId).Distinct().ToArray();
        var stockIds = selections.Select(x => x.StockId).Distinct().ToArray();
        var valid = (await db.ItemStocks.AsNoTracking().Where(x => itemIds.Contains(x.ItemRef) && stockIds.Contains(x.StockRef)
                && x.Stock.IsActive && x.Item.IsActive && x.Item.Sellable && !x.Item.SerialTracking &&
                !x.Item.IsPricingBasedOnTracing && x.Item.SecondaryUnitRef == null)
            .Select(x => new { x.ItemRef, x.StockRef }).ToListAsync(ct)).Select(x => (x.ItemRef, x.StockRef)).ToHashSet();
        var lines = new List<SepidarInvoiceExportItemPreview>(rows.Count);
        foreach (var row in rows)
        {
            if (!byId.TryGetValue(row.Id, out var selected) || !valid.Contains((selected.ItemId, selected.StockId)))
                throw new SalesInvoiceDestinationException($"کالا یا انبار قلم «{row.ProductTitle}» برای ثبت معتبر نیست.");
            lines.Add(new() {
                RowId = lines.Count + 1, ItemRef = selected.ItemId, StockRef = selected.StockId, Quantity = row.Quantity,
                Fee = row.Fee, Price = row.Price, Discount = row.Discount, Tax = row.Tax,
                NetPrice = row.Price - row.Discount + row.Tax,
                QuotationItemId = row.SuggestedItemId == selected.ItemId && row.SuggestedStockId == selected.StockId
                    ? row.QuotationItemId.GetValueOrDefault() : 0
            });
        }
        if (lines.Where(x => x.QuotationItemId > 0).GroupBy(x => x.QuotationItemId).Any(group => group.Count() > 1))
            throw new SalesInvoiceDestinationException("بیش از یک قلم به یک ردیف پیش‌فاکتور وصل شده است.");
        return lines;
    }

    private static async Task<IReadOnlyList<string>> GetSyncBlockersAsync(SepidarContextDbBase db, Factor source,
        EditSnapshot snapshot, CancellationToken ct)
    {
        if (!HasSyncDifference(source, snapshot.Invoice, snapshot.Rows.Select(DestinationLine)))
            return ["اقلام و مبالغ فاکتور همگام هستند."];
        var blockers = snapshot.Plan.Blockers.ToList();
        if (source.Returned || source.ParentReturnedId != null)
            blockers.Add("فاکتور برگشتی قابل همگام‌سازی نیست.");
        if (source.CurrencyType != CurrencyTypeEnum.ریال || snapshot.Invoice.Rate != 1 || snapshot.Invoice.CurrencyRef != 1 ||
            snapshot.Invoice.Addition != 0 || snapshot.Invoice.Duty != 0 || snapshot.Invoice.AgreementRef != null ||
            snapshot.Invoice.AdditionFactor_VatEffective != 0 || snapshot.Invoice.AdditionFactor_VatIneffective != 0 ||
            snapshot.Rows.Any(HasUnsupportedSyncRow))
            blockers.Add("فاکتور یا اقلام مقصد دارای تنظیمات ویژه‌ای هستند که باید در سپیدار بررسی شوند.");
        if (snapshot.Invoice.QuotationRef is int quoteId && !await db.Quotations.AnyAsync(x => x.QuotationId == quoteId
                && x.FiscalYearRef == snapshot.Invoice.FiscalYearRef && x.Date.Date <= snapshot.Invoice.Date.Date, ct))
            blockers.Add("تاریخ یا سال مالی فاکتور با پیش‌فاکتور مرتبط در سپیدار سازگار نیست.");
        var lines = source.FactorItems.Select(SourceLine).ToArray();
        if (lines.Length == 0 || lines.Any(x => x.Quantity <= 0 || decimal.Round(x.Quantity, 4) != x.Quantity ||
                x.Fee < 0 || x.Price < 0 || x.Total < 0 || x.Discount < 0 || x.Discount > x.Price || x.Tax < 0))
            blockers.Add("مقدار یا مبالغ بعضی از اقلام فاکتور ERP معتبر نیست.");
        if (new SalesInvoiceSyncAmounts(lines.Sum(x => x.Price), lines.Sum(x => x.Discount), lines.Sum(x => x.Tax),
                lines.Sum(x => x.Total)) != SourceAmounts(source))
            blockers.Add("جمع مبالغ ردیف‌ها با مبلغ فاکتور ERP برابر نیست.");
        if (blockers.Count == 0 && await HasSyncRowReferencesAsync(db, snapshot.Invoice.InvoiceId, ct, allowSalesDelivery: true))
            blockers.Add("یکی از اقلام فاکتور در سند دیگری در سپیدار استفاده شده است.");
        return blockers;
    }

    private static bool HasUnsupportedSyncRow(SalesInvoiceItemRecord row) =>
        row.TracingRef != null || row.SecondaryQuantity != null || row.ProductPackRef != null ||
        row.ProductPackQuantity != null || row.DiscountInvoiceItemRef != null || row.DiscountItemGroupRef != null ||
        row.IsAggregateDiscountInvoiceItem || row.Rate != 1 || row.Addition != 0 || row.Duty != 0 ||
        row.PriceInfoDiscountRate != 0 || row.PriceInfoPriceDiscount != 0 || row.PriceInfoPercentDiscount != 0 ||
        row.CustomerDiscount != 0 || row.CustomerDiscountRate != 0 || row.AggregateAmountDiscountRate != 0 ||
        row.AggregateAmountPriceDiscount != 0 || row.AggregateAmountPercentDiscount != 0 ||
        row.BankFeeForCurrencySale != 0 || row.TaxPayerCurrencyPurchaseRate != 0 ||
        row.AdditionFactor_VatEffective != 0 || row.AdditionFactor_VatIneffective != 0 ||
        !string.IsNullOrWhiteSpace(row.Description) || !string.IsNullOrWhiteSpace(row.Description_En);

    private static async Task<bool> HasSyncRowReferencesAsync(SepidarContextDbBase db, int id, CancellationToken ct, bool allowSalesDelivery = false)
    {
        var references = await db.Database.SqlQuery<SyncReference>($"""
            SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) AS SchemaName, OBJECT_NAME(f.parent_object_id) AS TableName,
                   COL_NAME(f.parent_object_id, f.parent_column_id) AS ColumnName,
                   COL_NAME(f.referenced_object_id, f.referenced_column_id) AS TargetColumnName
            FROM sys.foreign_key_columns f WHERE f.referenced_object_id = OBJECT_ID(N'SLS.InvoiceItem')
            """).ToListAsync(ct);
        using var identifiers = new SqlCommandBuilder();
        foreach (var reference in references)
        {
            if (allowSalesDelivery && reference is { SchemaName: "INV", TableName: "InventoryDeliveryItem", ColumnName: "BaseInvoiceItem" })
                continue;
            var table = identifiers.QuoteIdentifier(reference.SchemaName) + "." + identifiers.QuoteIdentifier(reference.TableName);
            var column = identifiers.QuoteIdentifier(reference.ColumnName);
            var target = identifiers.QuoteIdentifier(reference.TargetColumnName);
            var sql = $"SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM {table} r JOIN SLS.InvoiceItem i ON r.{column}=i.{target}"
                + " WHERE i.InvoiceRef=@invoiceId) THEN 1 ELSE 0 END AS bit) AS [Value]";
            if (await db.Database.SqlQueryRaw<bool>(sql, new SqlParameter("invoiceId", id)).SingleAsync(ct))
                return true;
        }
        return false;
    }

    private static async Task ReconcileSyncQuotationAsync(SepidarContextDbBase db, List<SalesInvoiceItemRecord> previous,
        List<SepidarInvoiceExportItemPreview> replacement, CancellationToken ct)
    {
        var ids = previous.Where(x => x.QuotationItemRef.HasValue).Select(x => x.QuotationItemRef!.Value)
            .Concat(replacement.Where(x => x.QuotationItemId > 0).Select(x => x.QuotationItemId)).Distinct().ToArray();
        if (ids.Length == 0) return;
        var rows = await db.QuotationItems.Where(x => ids.Contains(x.QuotationItemID)).ToListAsync(ct);
        if (rows.Count != ids.Length)
            throw new SalesInvoiceDestinationException("یکی از اقلام پیش‌فاکتور مرتبط در سپیدار پیدا نشد.");
        var totals = await (from item in db.SalesInvoiceItems.AsNoTracking()
            join invoice in db.SalesInvoices.AsNoTracking() on item.InvoiceRef equals invoice.InvoiceId
            where invoice.State == ActiveInvoice && item.QuotationItemRef.HasValue && ids.Contains(item.QuotationItemRef.Value)
            group item by new { Id = item.QuotationItemRef!.Value, item.ItemRef } into lines
            select new { lines.Key.Id, lines.Key.ItemRef, Quantity = lines.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => (x.Id, x.ItemRef), x => x.Quantity, ct);
        foreach (var row in rows)
            row.UsedQuantity = totals.GetValueOrDefault((row.QuotationItemID, row.ItemRef));
        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeSyncTitle(string? title) => (title ?? "").Trim().Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک');
    private static SalesInvoiceSyncAmounts SourceAmounts(Factor source) =>
        new(source.SumTotalAmount, source.Discount, source.TotalTax, source.PaymentAmount);
    private static SalesInvoiceSyncAmounts DestinationAmounts(SalesInvoiceRecord invoice) =>
        new(invoice.Price, invoice.Discount, invoice.Tax, invoice.NetPriceInBaseCurrency);
    private static bool HasSyncDifference(Factor source, SalesInvoiceRecord invoice, IEnumerable<SyncLineAmounts> lines) =>
        SourceAmounts(source) != DestinationAmounts(invoice) || !source.FactorItems.Select(SourceLine).SequenceEqual(lines);
    private static SyncLineAmounts SourceLine(FactorItem row) =>
        new(decimal.TryParse(row.WeightCount, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) ? quantity : 0,
            row.UnitAmount, row.TotalAmount + row.Discount - row.TaxAmount, row.Discount, row.TaxAmount, row.TotalAmount);
    private static SyncLineAmounts DestinationLine(SalesInvoiceItemRecord row) =>
        new(row.Quantity, row.Fee, row.Price, row.Discount, row.Tax, row.NetPriceInBaseCurrency);
    private static string GetSyncToken(EditSnapshot snapshot, Factor source, IReadOnlyList<SalesInvoiceSyncRow> rows) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            snapshot.Token, source.Id, source.PfactorID, source.CurrencyType, source.ParentReturnedId, source.Returned,
            Amounts = SourceAmounts(source), rows
        })));

    private sealed record SyncLineAmounts(decimal Quantity, decimal Fee, decimal Price, decimal Discount, decimal Tax, decimal Total);
    private sealed record SyncPresentation(IReadOnlyList<SalesInvoiceSyncRow> SourceRows, IReadOnlyList<SalesInvoiceSyncDestinationRow> DestinationRows);
    private sealed record SyncReference(string SchemaName, string TableName, string ColumnName, string TargetColumnName);
}
