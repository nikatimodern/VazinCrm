using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxDispatchQueue(CRMContextDb db, ITaxPreviewProtector protection) : ITaxDispatchQueue
{
    public async Task<TaxDispatchAttempt> ClaimAsync(Guid dispatchId, TimeSpan lease, CancellationToken ct)
    {
        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(lease));

        var now = DateTime.UtcNow;
        await ExpireAsync(dispatchId, now, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var attempt = Guid.NewGuid();
        var claimed = await db.TaxDispatches.Where(x => x.Id == dispatchId && x.TaxId == null && x.Status == TaxDispatchStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptId, attempt)
                .SetProperty(x => x.LeaseUntilUtc, now + lease)
                .SetProperty(x => x.Status, TaxDispatchStatus.InFlight), ct);
        if (claimed == 0)
        {
            await tx.CommitAsync(ct);
            return null;
        }

        var work = await (from entry in db.TaxDispatches
                          join snapshot in db.TaxInvoiceSnapshots on entry.SnapshotId equals snapshot.Id
                          where entry.Id == dispatchId
                          select new { entry.MemoryId, entry.FiscalId, snapshot.ProtectedPreview }).SingleAsync(ct);
        var preview = protection.Unprotect(work.ProtectedPreview);
        await tx.CommitAsync(ct);
        return new(dispatchId, attempt, work.MemoryId, work.FiscalId, preview);
    }

    public async Task<TaxDispatchState> GetStateAsync(Guid dispatchId, CancellationToken ct)
    {
        await ExpireAsync(dispatchId, DateTime.UtcNow, ct);
        return await db.TaxDispatches.Where(x => x.Id == dispatchId)
            .Select(x => new TaxDispatchState(x.Id, x.AttemptId, x.Status, x.TaxId, x.AuthorityReference)).SingleOrDefaultAsync(ct);
    }

    public async Task<bool> CompleteAsync(Guid dispatchId, Guid attemptId, TaxDispatchStatus status, CancellationToken ct)
    {
        if (status is not (TaxDispatchStatus.Unknown or TaxDispatchStatus.Accepted or TaxDispatchStatus.Rejected))
            throw new ArgumentOutOfRangeException(nameof(status));

        var affected = await db.TaxDispatches.Where(x => x.Id == dispatchId && x.TaxId == null && x.AttemptId == attemptId &&
                (x.Status == TaxDispatchStatus.InFlight ||
                 (x.Status == TaxDispatchStatus.Unknown && status != TaxDispatchStatus.Unknown)))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
        return affected == 1;
    }

    public Task MarkSubmissionUnknownAsync(Guid dispatchId, Guid attemptId, CancellationToken ct) =>
        TaxDispatchEventWriter.RecordAsync(db, dispatchId, attemptId, TaxDispatchEventKind.Unknown, "Ambiguous",
            () => db.TaxDispatches.Where(x => x.Id == dispatchId && x.TaxId != null && x.AttemptId == attemptId && x.Status == TaxDispatchStatus.InFlight)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.Unknown)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct), ct);

    private Task ExpireAsync(Guid dispatchId, DateTime utcNow, CancellationToken ct) =>
        db.TaxDispatches.Where(x => x.Id == dispatchId && x.Status == TaxDispatchStatus.InFlight && x.LeaseUntilUtc <= utcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.Unknown)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
}
