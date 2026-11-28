using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Repositories;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.PreFactors.Repositories;

public sealed partial class PreFactorDestinationRepository
{
    public async Task<bool> HasSyncDifferenceAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var (destination, _, id, sourceNumber) = await ResolveAsync(key, ct);
        var quote = await destination.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.QuotationId == id, ct)
            ?? throw new PreFactorDestinationException("پیش‌فاکتور در نرم‌افزار مقصد پیدا نشد.");
        CheckReference(quote, sourceNumber);
        var items = await destination.QuotationItems.AsNoTracking().Where(x => x.QuotationRef == id)
            .OrderBy(x => x.RowID).ThenBy(x => x.QuotationItemID)
            .Select(x => new LineValues(x.Quantity, x.Fee, x.Price, x.Discount, x.Tax ?? 0))
            .ToListAsync(ct);
        return await HasSyncDifferenceAsync(key.SourceId, quote, items, ct);
    }

    public async Task<PreFactorSyncPreview> GetSyncPreviewAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var state = await ReadSyncStateAsync(key, false, ct);
        var blockers = HasSyncDifference(state)
            ? await GetSyncBlockersAsync(state, ct)
            : ["اقلام و مبالغ پیش‌فاکتور همگام هستند."];
        var suggestions = state.DestinationRows.Where(row => NormalizeTitle(row.Title).Length != 0)
            .GroupBy(row => NormalizeTitle(row.Title), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(row => (row.Item.ItemRef, row.Item.StockRef))
                .Distinct().Take(2).ToArray(), StringComparer.Ordinal);
        var sourceRows = state.SourceRows.Select(row => {
            var match = suggestions.GetValueOrDefault(NormalizeTitle(row.Product));
            return new PreFactorSyncSourceRow(row.Id, $"{row.ProductSmallName} {row.Product}".Trim(), row.UnitTxt,
                SourceValues(row).Quantity, row.UnitAmount, row.TotalAmount,
                row.Discount, row.TaxAmount, match?.Length == 1 ? match[0].ItemRef : null,
                match?.Length == 1 ? match[0].StockRef : null);
        }).ToArray();
        var destinationRows = state.DestinationRows.Select(row => new PreFactorSyncDestinationRow(row.Item.RowID,
            row.Title, row.ItemCode, row.Stock, row.StockCode,
            row.Item.Quantity, row.Item.Fee, row.Item.Price, row.Item.Discount, row.Item.Tax ?? 0)).ToArray();
        return new(state.Quote.Number, state.SoftwareTitle, SyncToken(key, state), blockers, sourceRows, destinationRows,
            SourceAmounts(state.Source), DestinationAmounts(state.Quote));
    }

    public async Task<bool> SynchronizeAsync(PreFactorSyncRequest request, int externalUserId, CancellationToken ct)
    {
        await using var sourceTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (destination, _, _, _) = await ResolveAsync(request.Key, ct);
        await using var transaction = await destination.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireSyncLockAsync(destination, ct);
        var state = await ReadSyncStateAsync(request.Key, true, ct);
        if (SyncToken(request.Key, state) != request.Token)
            throw new PreFactorDestinationException("اطلاعات ERP یا سپیدار تغییر کرده است. پیش‌نمایش را دوباره باز کنید.");
        if (!HasSyncDifference(state))
            return false;
        var blockers = await GetSyncBlockersAsync(state, ct);
        if (blockers.Count > 0)
            throw new PreFactorDestinationException(blockers[0]);

        var selections = request.Items;
        if (selections == null || selections.Count != state.SourceRows.Count ||
            selections.Any(x => x == null || x.ItemId <= 0 || x.StockId <= 0) ||
            selections.Select(x => x.SourceRowId).Distinct().Count() != selections.Count)
            throw new PreFactorDestinationException("برای همه اقلام، کالا و انبار معتبر انتخاب کنید.");
        var byRow = selections.ToDictionary(x => x.SourceRowId);
        if (state.SourceRows.Any(row => !byRow.ContainsKey(row.Id)))
            throw new PreFactorDestinationException("انتخاب‌های کالا و انبار با اقلام فعلی ERP مطابقت ندارند.");

        var ordered = state.SourceRows.Select(row => byRow[row.Id]).ToArray();
        await ValidateSyncSelectionsAsync(destination, ordered, ct);
        var lastId = await AllocateSyncRowIdsAsync(destination, ordered.Length, ct);
        var quoteId = state.Quote.QuotationId;
        await destination.QuotationItems.Where(row => row.QuotationRef == quoteId).ExecuteDeleteAsync(ct);
        destination.QuotationItems.AddRange(state.SourceRows.Select((row, index) => QuotationExportValues.CreateItem(
            checked(lastId - ordered.Length + index + 1), quoteId, index + 1, ordered[index].ItemId, ordered[index].StockId,
            (int)SourceValues(row).Quantity, row)));
        QuotationExportValues.ApplyAmounts(state.Quote, state.Source);
        state.Quote.LastModifier = externalUserId;
        state.Quote.LastModificationDate = DateTime.Now;
        state.Quote.Version = checked(state.Quote.Version + 1);
        await destination.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<SyncState> ReadSyncStateAsync(PreFactorDestinationKey key, bool lockQuote, CancellationToken ct)
    {
        var (destination, mapping, id, sourceNumber) = await ResolveAsync(key, ct);
        var source = await ReadSyncSourceAsync(key.SourceId, ct);
        var quote = (lockQuote ? await LockAsync(destination, id, ct)
            : await destination.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.QuotationId == id, ct))
            ?? throw new PreFactorDestinationException("پیش‌فاکتور در نرم‌افزار مقصد پیدا نشد.");
        CheckReference(quote, sourceNumber);
        var sourceRows = await ReadSyncSourceRowsAsync(key.SourceId, ct);
        var destinationRows = await (from row in destination.QuotationItems.AsNoTracking()
            join item in destination.Items.AsNoTracking() on row.ItemRef equals item.ItemID into items
            from item in items.DefaultIfEmpty()
            join stock in destination.Stocks.AsNoTracking() on row.StockRef equals stock.StockID into stocks
            from stock in stocks.DefaultIfEmpty()
            where row.QuotationRef == id
            orderby row.RowID, row.QuotationItemID
            select new SyncDestinationRow(row, item == null ? "" : item.Title, item == null ? null : item.Code,
                stock == null ? "" : stock.Title, stock == null ? null : (int?)stock.Code))
            .ToListAsync(ct);
        return new(destination, mapping.ExternalSoftware.Title, source, quote, sourceRows, destinationRows);
    }

    private async Task<PreFactor> ReadSyncSourceAsync(Guid sourceId, CancellationToken ct) =>
        await crm.PreFactors.AsNoTracking().Where(x => x.Id == sourceId && !x.IsDeleted)
            .Select(x => new PreFactor {
                SumTotalAmount = x.SumTotalAmount, Discount = x.Discount, TotalTax = x.TotalTax, PaymentAmount = x.PaymentAmount
            }).SingleOrDefaultAsync(ct)
        ?? throw new PreFactorDestinationException("پیش‌فاکتور فعال در ERP پیدا نشد.");

    private Task<List<PreFactorItem>> ReadSyncSourceRowsAsync(Guid sourceId, CancellationToken ct) =>
        crm.PreFactorItems.AsNoTracking().Where(x => x.PrefactorId == sourceId && !x.IsDeleted)
            .OrderBy(x => x.Row).ThenBy(x => x.Id)
            .Select(x => new PreFactorItem {
                Id = x.Id, Product = x.Product, ProductSmallName = x.ProductSmallName, ProductCode = x.ProductCode,
                WeightCount = x.WeightCount, UnitTxt = x.UnitTxt,
                UnitAmount = x.UnitAmount, TotalAmount = x.TotalAmount, Discount = x.Discount, TaxAmount = x.TaxAmount
            }).ToListAsync(ct);

    private async Task<bool> HasSyncDifferenceAsync(Guid sourceId, Quotation quote, IEnumerable<LineValues> items, CancellationToken ct)
    {
        var source = await ReadSyncSourceAsync(sourceId, ct);
        return SourceAmounts(source) != DestinationAmounts(quote)
            || !(await ReadSyncSourceRowsAsync(sourceId, ct)).Select(SourceValues).SequenceEqual(items);
    }

    private static async Task<IReadOnlyList<string>> GetSyncBlockersAsync(SyncState state, CancellationToken ct)
    {
        var blockers = new List<string>();
        if (await GetRestrictionAsync(state.Destination, state.Quote, ct) is { } restriction)
            blockers.Add(restriction);
        if (state.Quote.Closed)
            blockers.Add("پیش‌فاکتور مقصد بسته شده است.");
        var dependencies = await GetDeleteBlockersAsync(state.Destination, state.Quote, ct);
        if (dependencies.Length > 0)
        {
            var invoices = dependencies.Where(dependency => dependency.StartsWith("فاکتور فروش شماره ", StringComparison.Ordinal)).ToArray();
            var otherDependencies = dependencies.Except(invoices).ToArray();
            var instructions = new List<string>();
            if (invoices.Length > 0)
                instructions.Add($"ابتدا {string.Join("، ", invoices)} را در سپیدار حذف کنید.");
            if (otherDependencies.Length > 0)
                instructions.Add($"وابستگی این اسناد را در سپیدار برطرف کنید: {string.Join("، ", otherDependencies)}.");
            instructions.Add("سپس دوباره همگام‌سازی کنید.");
            blockers.Add(string.Join(" ", instructions));
            return blockers;
        }
        if (state.SourceRows.Count == 0)
            blockers.Add("پیش‌فاکتور ERP قلم فعالی برای همگام‌سازی ندارد.");
        if (state.SourceRows.FirstOrDefault(row => SourceValues(row).Quantity <= 0) is { } invalid)
            blockers.Add($"تعداد قلم «{invalid.Product}» برای ثبت در سپیدار معتبر نیست.");
        if (state.Quote.CurrencyRef != 1 || state.Quote.Rate != 1 || state.Quote.Addition.GetValueOrDefault() != 0 ||
            state.Quote.Duty.GetValueOrDefault() != 0 || state.Quote.AdditionFactor_VatEffective.GetValueOrDefault() != 0 ||
            state.Quote.AdditionFactor_VatIneffective.GetValueOrDefault() != 0)
            blockers.Add("برای اصلاح مبلغ، ارز یا عوارض این پیش‌فاکتور، سند را در سپیدار بررسی و اصلاح کنید.");
        if (state.DestinationRows.FirstOrDefault(row => HasUnsupportedItemData(row.Item)) is { } restrictedRow)
            blockers.Add($"قلم ردیف {restrictedRow.Item.RowID} را در خود سپیدار بررسی و اصلاح کنید. این ردیف قابل جایگزینی خودکار نیست.");
        if (blockers.Count == 0 && await HasOtherSyncReferencesAsync(state.Destination, state.Quote.QuotationId, ct))
            blockers.Add("یکی از اقلام پیش‌فاکتور در رکورد دیگری در سپیدار استفاده شده است.");
        return blockers;
    }

    private static bool HasUnsupportedItemData(QuotationItem row) =>
        row.UsedQuantity.GetValueOrDefault() != 0 || row.TracingRef != null || row.SecondaryQuantity != null ||
        row.ProductPackRef != null || row.ProductPackQuantity != null || row.DiscountQuotationItemRef != null ||
        !string.IsNullOrWhiteSpace(row.Description) || !string.IsNullOrWhiteSpace(row.Description_En) || row.Rate != 1 ||
        !row.IsReadyForProductOrder || row.IsAggregateDiscountInvoiceItem ||
        row.Addition.GetValueOrDefault() != 0 || row.Duty.GetValueOrDefault() != 0 ||
        row.PriceInfoDiscountRate != 0 || row.PriceInfoPriceDiscount != 0 || row.PriceInfoPercentDiscount != 0 ||
        row.CustomerDiscount != 0 || row.CustomerDiscountRate != 0 || row.AggregateAmountDiscountRate != 0 ||
        row.AggregateAmountPriceDiscount != 0 || row.AggregateAmountPercentDiscount != 0 ||
        row.AdditionFactor_VatEffective.GetValueOrDefault() != 0 || row.AdditionFactor_VatIneffective.GetValueOrDefault() != 0;

    private static async Task<bool> HasOtherSyncReferencesAsync(SepidarContextDbBase destination, int id, CancellationToken ct)
    {
        var references = await destination.Database.SqlQuery<SyncForeignKey>($"""
            SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) AS SchemaName, OBJECT_NAME(f.parent_object_id) AS TableName,
                   COL_NAME(f.parent_object_id, f.parent_column_id) AS ColumnName,
                   COL_NAME(f.referenced_object_id, f.referenced_column_id) AS TargetColumnName
            FROM sys.foreign_key_columns f WHERE f.referenced_object_id = OBJECT_ID(N'SLS.QuotationItem')
            """).ToListAsync(ct);
        using var identifiers = new SqlCommandBuilder();
        foreach (var reference in references)
        {
            var table = identifiers.QuoteIdentifier(reference.SchemaName) + "." + identifiers.QuoteIdentifier(reference.TableName);
            var column = identifiers.QuoteIdentifier(reference.ColumnName);
            var target = identifiers.QuoteIdentifier(reference.TargetColumnName);
            var sql = $"SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM {table} r JOIN SLS.QuotationItem q ON r.{column}=q.{target}"
                + " WHERE q.QuotationRef=@quotationId) THEN 1 ELSE 0 END AS bit) AS [Value]";
            if (await destination.Database.SqlQueryRaw<bool>(sql, new SqlParameter("quotationId", id)).SingleAsync(ct))
                return true;
        }
        return false;
    }

    private static async Task ValidateSyncSelectionsAsync(SepidarContextDbBase destination, PreFactorSyncSelection[] ordered, CancellationToken ct)
    {
        var itemIds = ordered.Select(x => x.ItemId).Distinct().ToArray();
        var stockIds = ordered.Select(x => x.StockId).Distinct().ToArray();
        var options = await destination.ItemStocks.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemRef) && stockIds.Contains(x.StockRef) &&
                x.Item.IsActive && x.Item.Sellable && x.Stock.IsActive)
            .Select(x => new { x.ItemRef, x.StockRef }).ToListAsync(ct);
        var valid = options.Select(x => (x.ItemRef, x.StockRef)).ToHashSet();
        if (ordered.Any(x => !valid.Contains((x.ItemId, x.StockId))))
            throw new PreFactorDestinationException("انبار یا کالای انتخاب‌شده برای یکی از اقلام در سپیدار معتبر نیست.");
    }

    private static async Task AcquireSyncLockAsync(SepidarContextDbBase destination, CancellationToken ct)
    {
        var result = new SqlParameter("result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await destination.Database.ExecuteSqlRawAsync("""
            EXEC @result = sys.sp_getapplock @Resource='CRM.Sepidar.InvoiceAndDelivery',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000;
            """, new[] { result }, ct);
        if (result.Value is not int status || status < 0)
            throw new PreFactorDestinationException("ثبت سند دیگری در سپیدار در جریان است. دوباره تلاش کنید.");
    }

    private static async Task<int> AllocateSyncRowIdsAsync(SepidarContextDbBase destination, int count, CancellationToken ct)
    {
        var id = new SqlParameter("id", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await destination.Database.ExecuteSqlRawAsync("EXEC FMK.spGetNextId @TableName='SLS.QuotationItem', @Id=@id OUTPUT, @IncValue=@count;",
            new[] { id, new SqlParameter("count", count) }, ct);
        return id.Value is int last && last >= count ? last
            : throw new PreFactorDestinationException("شناسه اقلام در سپیدار تخصیص داده نشد.");
    }

    private static string NormalizeTitle(string? title) => (title ?? "").Trim().Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک');
    private static PreFactorSyncAmounts SourceAmounts(PreFactor source) =>
        new(source.SumTotalAmount, source.Discount, source.TotalTax, source.PaymentAmount);
    private static PreFactorSyncAmounts DestinationAmounts(Quotation quote) =>
        new(quote.Price, quote.Discount ?? 0, quote.Tax ?? 0, quote.NetPriceInBaseCurrency);
    private static bool HasSyncDifference(SyncState state) =>
        SourceAmounts(state.Source) != DestinationAmounts(state.Quote) ||
        !state.SourceRows.Select(SourceValues).SequenceEqual(state.DestinationRows.Select(row => DestinationValues(row.Item)));
    private static LineValues SourceValues(PreFactorItem row) =>
        new(int.TryParse(row.WeightCount, out var weight) ? weight : 0, row.UnitAmount, row.TotalAmount, row.Discount, row.TaxAmount);
    private static LineValues DestinationValues(QuotationItem row) =>
        new(row.Quantity, row.Fee, row.Price, row.Discount, row.Tax ?? 0);
    private static string SyncToken(PreFactorDestinationKey key, SyncState state) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            key, state.Quote.Version, state.Quote.LastModificationDate,
            Source = SourceAmounts(state.Source), Destination = DestinationAmounts(state.Quote),
            SourceRows = state.SourceRows.Select(row => new {
                row.Id, row.ProductCode, row.Product, row.ProductSmallName, row.UnitTxt, Values = SourceValues(row)
            }),
            DestinationRows = state.DestinationRows.Select(row => new { row.Item.QuotationItemID, row.Item.RowID,
                row.Item.ItemRef, row.Item.StockRef, row.Title, row.ItemCode, row.Stock, row.StockCode,
                Values = DestinationValues(row.Item) })
        })));

    private sealed record LineValues(decimal Quantity, decimal Fee, decimal Price, decimal Discount, decimal Tax);
    private sealed record SyncDestinationRow(QuotationItem Item, string Title, string? ItemCode, string Stock, int? StockCode);
    private sealed record SyncState(SepidarContextDbBase Destination, string SoftwareTitle, PreFactor Source, Quotation Quote,
        IReadOnlyList<PreFactorItem> SourceRows, IReadOnlyList<SyncDestinationRow> DestinationRows);
    private sealed record SyncForeignKey(string SchemaName, string TableName, string ColumnName, string TargetColumnName);
}
