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
    public async Task<bool> HasOpenSubmissionsAsync(TaxAuthorityProtocol protocol, CancellationToken ct) =>
        await db.TaxDispatches.AnyAsync(x => x.AuthorityProtocol == protocol &&
            (x.Status == TaxDispatchStatus.InFlight || x.Status == TaxDispatchStatus.Submitted ||
             x.Status == TaxDispatchStatus.Unknown || (x.Status == TaxDispatchStatus.Pending && x.TaxId != null)), ct) ||
        (protocol == TaxAuthorityProtocol.V2 && await db.TaxPaymentDispatches.AnyAsync(x =>
            x.Status == TaxPaymentStatus.Pending || x.Status == TaxPaymentStatus.InFlight || x.Status == TaxPaymentStatus.Unknown, ct));

    public async Task<TaxSubmissionDraft> ReadDraftAsync(Guid dispatchId, CancellationToken ct)
    {
        var draft = await (from item in db.TaxDispatches
                           join snapshot in db.TaxInvoiceSnapshots on item.SnapshotId equals snapshot.Id
                           where item.Id == dispatchId && item.Kind == TaxDispatchKind.Original &&
                               item.MemoryId == snapshot.TaxMemoryId && item.FactorId == snapshot.FactorId
                           select new { item.Id, item.MemoryId, item.FiscalId, snapshot.ProtectedPreview })
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
        return new(draft.Id, draft.MemoryId, draft.FiscalId, protection.Unprotect(draft.ProtectedPreview));
    }

    public async Task<TaxSubmission> FindAsync(Guid dispatchId, CancellationToken ct)
    {
        var item = await db.TaxDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dispatchId &&
            db.TaxInvoiceSnapshots.Any(snapshot => snapshot.Id == x.SnapshotId &&
                snapshot.TaxMemoryId == x.MemoryId && snapshot.FactorId == x.FactorId), ct)
            ?? throw new KeyNotFoundException();
        if (!item.AuthorityEnvironment.HasValue)
            return null;
        TaxSubmissionReference reference = null;
        if (item.Status == TaxDispatchStatus.Pending && item.ReferencedDispatchId.HasValue)
        {
            var previous = await db.TaxDispatches.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == item.ReferencedDispatchId && x.MemoryId == item.MemoryId &&
                x.AuthorityEnvironment == item.AuthorityEnvironment, ct);
            if (string.IsNullOrEmpty(previous?.ProtectedWorkFolderEvidence))
                throw new ArgumentException("مدرک وضعیت کارپوشهٔ مرجع در دسترس نیست.");
            reference = new(previous.TaxId, previous.Kind, previous.Status,
                protection.Unprotect(previous.ProtectedWorkFolderEvidence), previous.ProtectedWorkFolderEvidence);
        }
        return new(item.Id, item.FiscalId, item.AuthorityEnvironment.Value, item.CertificateSha256,
            item.TaxId, item.Status == TaxDispatchStatus.Pending ? protection.Unprotect(item.ProtectedSubmission) : null,
            item.AttemptId, item.SubmissionStartedAt, item.AuthorityReference, item.Status, item.InvoiceSerial, item.Kind, reference,
            item.AuthorityProtocol, item.V1KeyFingerprint, item.MemoryId);
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
                    .SetProperty(x => x.AuthorityProtocol, binding.Protocol)
                    .SetProperty(x => x.V1KeyFingerprint, binding.V1KeyFingerprint)
                    .SetProperty(x => x.ProtectedSubmission, encrypted), ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ArgumentException("این شماره سریال در حافظه و محیط مالیاتی ثبت شده است.");
        }
    }

    public Task<bool> TryStartAsync(Guid dispatchId, Guid attemptId, DateTimeOffset startedAt, string referenceEvidenceVersion, CancellationToken ct) =>
        TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, TaxDispatchEventKind.SubmissionStarted, "Started",
            () => db.TaxDispatches.Where(x => x.Id == dispatchId && x.TaxId != null && x.Status == TaxDispatchStatus.Pending &&
                (x.ReferencedDispatchId == null || (referenceEvidenceVersion != null && db.TaxDispatches.Any(reference =>
                    reference.Id == x.ReferencedDispatchId && reference.Status == TaxDispatchStatus.Accepted &&
                    reference.Kind != TaxDispatchKind.Cancellation &&
                    reference.ProtectedWorkFolderEvidence == referenceEvidenceVersion))))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.InFlight)
                .SetProperty(x => x.AttemptId, attemptId)
                .SetProperty(x => x.SubmissionStartedAt, startedAt)
                .SetProperty(x => x.LeaseUntilUtc, startedAt.UtcDateTime.AddMinutes(2)), ct), ct);

    public async Task RecordAcknowledgementAsync(Guid dispatchId, Guid attemptId, Guid reference, string evidence, CancellationToken ct)
    {
        var encrypted = protection.Protect(evidence);
        await TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, TaxDispatchEventKind.Acknowledgement, "Received",
            () => ForAttempt(dispatchId, attemptId)
            .Where(x => x.Status == TaxDispatchStatus.InFlight || x.Status == TaxDispatchStatus.Unknown)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AuthorityReference, reference)
                .SetProperty(x => x.ProtectedAcknowledgement, encrypted)
                .SetProperty(x => x.Status, TaxDispatchStatus.Submitted)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct), ct);
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
        await TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, TaxDispatchEventKind.Processing, status,
            () => ForAttempt(dispatchId, attemptId).Where(x => x.Status == TaxDispatchStatus.Unknown || x.Status == TaxDispatchStatus.Submitted)
            .Where(x => reference == null || x.AuthorityReference == null || x.AuthorityReference == reference)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AuthorityReference, x => reference ?? x.AuthorityReference)
                .SetProperty(x => x.AuthorityProcessingStatus, status)
                .SetProperty(x => x.ProtectedProcessingEvidence, encrypted)
                .SetProperty(x => x.Status, x => next == TaxDispatchStatus.Unknown &&
                    x.Status == TaxDispatchStatus.Submitted ? x.Status : next), ct), ct);
    }

    public async Task RecordWorkFolderAsync(Guid dispatchId, Guid attemptId, string evidence, CancellationToken ct)
    {
        var encrypted = protection.Protect(evidence);
        await TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, TaxDispatchEventKind.WorkFolder, "Received",
            () => ForAttempt(dispatchId, attemptId).Where(x => x.Status == TaxDispatchStatus.Accepted)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProtectedWorkFolderEvidence, encrypted), ct), ct);
    }

    public Task RecordInquiryWithoutResultAsync(Guid dispatchId, Guid attemptId, TaxDispatchEventKind kind, CancellationToken ct)
    {
        if (kind is not (TaxDispatchEventKind.Processing or TaxDispatchEventKind.WorkFolder))
            throw new ArgumentOutOfRangeException(nameof(kind));
        return TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, kind, "NoResult",
            async () => await ForAttempt(dispatchId, attemptId).AnyAsync(ct) ? 1 : 0, ct);
    }

    private IQueryable<TaxDispatch> ForAttempt(Guid dispatchId, Guid attemptId) =>
        db.TaxDispatches.Where(x => x.Id == dispatchId && x.AttemptId == attemptId && x.TaxId != null);
}
