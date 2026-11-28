using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxInvoiceListing(CRMContextDb db, ITaxPreviewProtector protection = null) : ITaxInvoiceListing
{
    public async Task<TaxDispatchDetails> ReadDispatchAsync(Guid id, CancellationToken ct, long? historyBefore = null)
    {
        var now = DateTime.UtcNow;
        var dispatch = await db.TaxDispatches.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.SnapshotId, x.MemoryId, x.Kind, x.AuthorityProtocol, x.TaxId, x.AttemptId, x.AuthorityReference, x.QueuedUtc,
                x.SubmissionStartedAt, x.AuthorityProcessingStatus,
                Status = x.Status == TaxDispatchStatus.InFlight && x.LeaseUntilUtc <= now
                    ? TaxDispatchStatus.Unknown : x.Status,
                HasAcknowledgement = x.ProtectedAcknowledgement != null,
                x.ProtectedWorkFolderEvidence
            }).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
        const int historyPageSize = 50;
        var events = db.TaxDispatchEvents.AsNoTracking().Where(x => x.DispatchId == id);
        if (historyBefore is long before)
            events = events.Where(x => x.Id < before);
        var history = await events
            .OrderByDescending(x => x.Id).Take(historyPageSize + 1).ToListAsync(ct);
        long? olderBefore = null;
        if (history.Count > historyPageSize)
        {
            history.RemoveAt(historyPageSize);
            olderBefore = history[^1].Id;
        }
        using var workFolder = dispatch.ProtectedWorkFolderEvidence == null || protection == null ? null :
            JsonDocument.Parse(protection.Unprotect(dispatch.ProtectedWorkFolderEvidence));
        return new(dispatch.Id, dispatch.SnapshotId, dispatch.MemoryId, dispatch.Kind, dispatch.Status, dispatch.TaxId,
            dispatch.AttemptId, dispatch.AuthorityReference, dispatch.HasAcknowledgement, dispatch.QueuedUtc,
            dispatch.SubmissionStartedAt, dispatch.AuthorityProcessingStatus, dispatch.ProtectedWorkFolderEvidence != null,
            ReadStatus(workFolder, "invoiceStatus"), ReadStatus(workFolder, "article6Status"), history, olderBefore, dispatch.AuthorityProtocol);
    }

    private static string ReadStatus(JsonDocument evidence, string key) =>
        evidence?.RootElement is { ValueKind: JsonValueKind.Object } value &&
        value.TryGetProperty(key, out var status) && status.ValueKind == JsonValueKind.String ? status.GetString() : null;

    public async Task<TaxInvoicePage> ListAsync(TaxInvoiceFilter filter, CancellationToken ct)
    {
        var dispatches = db.TaxDispatches.AsNoTracking();
        var factors = db.Factors.AsNoTracking();
        var candidates = factors.Where(factor => !factor.IsDeleted &&
            ((factor.OfficialType == 1 && factor.status == FactorStatus.تایید_شده &&
                factor.ParentReturnedId == null) || dispatches.Any(x => x.FactorId == factor.Id)));
        if (filter.Search.Length > 0)
            candidates = candidates.Where(x => x.Customer.Contains(filter.Search) || x.SerialNumber == filter.InvoiceNumber);
        if (filter.Export.HasValue)
            candidates = candidates.Where(x => (x.Export ?? false) == filter.Export.Value);
        if (filter.Status.HasValue)
            candidates = candidates.Where(factor => dispatches.Any(x => x.FactorId == factor.Id && x.Status == filter.Status.Value));
        if (filter.Unqueued)
            candidates = candidates.Where(factor => !dispatches.Any(x => x.FactorId == factor.Id));

        var count = await candidates.CountAsync(ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(count / (decimal)filter.PageSize));
        var page = Math.Min(pageCount, filter.Page);
        var invoices = await candidates.OrderByDescending(x => x.DateMiladi).ThenByDescending(x => x.SerialNumber)
            .ThenBy(x => x.Id).Skip((page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new
            {
                x.Id, x.SerialNumber, x.Customer, x.DateMiladi, x.PaymentAmount, x.CurrencyType,
                Export = x.Export == true, IsReturn = x.ParentReturnedId != null
            }).ToListAsync(ct);
        var ids = invoices.Select(x => x.Id).ToArray();
        if (ids.Length == 0)
            return new([], page, pageCount, count);

        var documents = await (from document in dispatches
            where ids.Contains(document.FactorId)
            join original in dispatches on document.ReferencedDispatchId equals (Guid?)original.Id into originals
            from original in originals.DefaultIfEmpty()
            join source in factors on original.FactorId equals source.Id into sources
            from source in sources.DefaultIfEmpty()
            orderby document.QueuedUtc descending, document.Id
            select new TaxInvoiceDocument(document.Id, document.FactorId, document.Kind, document.Status,
                document.TaxId, original.TaxId, (int?)source.SerialNumber)).ToListAsync(ct);
        var byFactor = documents.ToLookup(x => x.FactorId);
        return new(invoices.Select(x => new TaxInvoiceListItem(x.Id, x.SerialNumber, x.Customer, x.DateMiladi,
            x.PaymentAmount, x.CurrencyType.ToString().Replace('_', ' '), x.Export, x.IsReturn,
            byFactor[x.Id].ToArray())).ToArray(), page, pageCount, count);
    }
}
