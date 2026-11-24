using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxSubmissionRepository(CRMContextDb db, ITaxPreviewProtector protection) : ITaxSubmissionRepository
{
    public async Task<TaxSubmissionDraft> ReadDraftAsync(Guid dispatchId, CancellationToken ct)
    {
        var draft = await (from item in db.TaxDispatches
                           join snapshot in db.TaxInvoiceSnapshots on item.SnapshotId equals snapshot.Id
                           where item.Id == dispatchId && item.Kind == TaxDispatchKind.Original
                           select new { item.Id, item.MemoryId, item.FiscalId, snapshot.ProtectedPreview })
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
        return new(draft.Id, draft.MemoryId, draft.FiscalId, protection.Unprotect(draft.ProtectedPreview));
    }

    public async Task<TaxSubmission> FindAsync(Guid dispatchId, CancellationToken ct)
    {
        var item = await db.TaxDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dispatchId, ct)
            ?? throw new KeyNotFoundException();
        if (!item.AuthorityEnvironment.HasValue)
            return null;
        return new(item.Id, item.FiscalId, item.AuthorityEnvironment.Value, item.CertificateSha256,
            item.TaxId, item.Status == TaxDispatchStatus.Pending ? protection.Unprotect(item.ProtectedSubmission) : null,
            item.AttemptId, item.SubmissionStartedAt, item.AuthorityReference, item.Status);
    }

    public async Task PinAsync(Guid dispatchId, TaxSigningBinding binding, long serial, string taxId, string payload, CancellationToken ct)
    {
        var encrypted = protection.Protect(payload);
        try
        {
            await db.TaxDispatches.Where(x => x.Id == dispatchId && x.Status == TaxDispatchStatus.Pending && x.TaxId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TaxId, taxId)
                    .SetProperty(x => x.InvoiceSerial, serial)
                    .SetProperty(x => x.AuthorityEnvironment, binding.Environment)
                    .SetProperty(x => x.CertificateSha256, binding.CertificateSha256)
                    .SetProperty(x => x.ProtectedSubmission, encrypted), ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ArgumentException("این شماره سریال در حافظه و محیط مالیاتی ثبت شده است.");
        }
    }

    public async Task<bool> TryStartAsync(Guid dispatchId, Guid attemptId, DateTimeOffset startedAt, CancellationToken ct) =>
        await db.TaxDispatches.Where(x => x.Id == dispatchId && x.TaxId != null && x.Status == TaxDispatchStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.InFlight)
                .SetProperty(x => x.AttemptId, attemptId)
                .SetProperty(x => x.SubmissionStartedAt, startedAt)
                .SetProperty(x => x.LeaseUntilUtc, startedAt.UtcDateTime.AddMinutes(2)), ct) == 1;

    public async Task RecordAcknowledgementAsync(Guid dispatchId, Guid attemptId, Guid reference, string evidence, CancellationToken ct)
    {
        var encrypted = protection.Protect(evidence);
        await ForAttempt(dispatchId, attemptId)
            .Where(x => x.Status == TaxDispatchStatus.InFlight || x.Status == TaxDispatchStatus.Unknown)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AuthorityReference, reference)
                .SetProperty(x => x.ProtectedAcknowledgement, encrypted)
                .SetProperty(x => x.Status, TaxDispatchStatus.Submitted)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
    }

    public async Task RecordProcessingAsync(Guid dispatchId, Guid attemptId, Guid? reference, string status, string evidence, CancellationToken ct)
    {
        var encrypted = protection.Protect(evidence);
        var next = status switch
        {
            "SUCCESS" => TaxDispatchStatus.Accepted,
            "FAILED" => TaxDispatchStatus.Rejected,
            "IN_PROGRESS" => TaxDispatchStatus.Submitted,
            "TIMEOUT" => TaxDispatchStatus.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        await ForAttempt(dispatchId, attemptId).Where(x => x.Status == TaxDispatchStatus.Unknown || x.Status == TaxDispatchStatus.Submitted)
            .Where(x => reference == null || x.AuthorityReference == null || x.AuthorityReference == reference)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AuthorityReference, x => reference ?? x.AuthorityReference)
                .SetProperty(x => x.AuthorityProcessingStatus, status)
                .SetProperty(x => x.ProtectedProcessingEvidence, encrypted)
                .SetProperty(x => x.Status, next), ct);
    }

    public async Task RecordWorkFolderAsync(Guid dispatchId, Guid attemptId, string evidence, CancellationToken ct)
    {
        var encrypted = protection.Protect(evidence);
        await ForAttempt(dispatchId, attemptId).Where(x => x.Status == TaxDispatchStatus.Accepted)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProtectedWorkFolderEvidence, encrypted), ct);
    }

    private IQueryable<TaxDispatch> ForAttempt(Guid dispatchId, Guid attemptId) =>
        db.TaxDispatches.Where(x => x.Id == dispatchId && x.AttemptId == attemptId && x.TaxId != null);
}
