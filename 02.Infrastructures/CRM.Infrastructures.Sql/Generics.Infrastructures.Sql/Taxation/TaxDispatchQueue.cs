using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxDispatchQueue(CRMContextDb db, ITaxPreviewProtector protection) : ITaxDispatchQueue
{
    public async Task<TaxDispatchAttempt> ClaimAsync(TimeSpan lease, CancellationToken ct)
    {
        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(lease));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await TaxationRepository.LockAsync(db, "tax-dispatch-claim", ct);
        var now = DateTime.UtcNow;
        await db.TaxDispatches.Where(x => x.Status == TaxDispatchStatus.InFlight && x.LeaseUntilUtc <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.Unknown)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
        var entry = await db.TaxDispatches.AsNoTracking().Where(x => x.Status == TaxDispatchStatus.Pending)
            .OrderBy(x => x.QueuedUtc).ThenBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (entry is null)
        {
            await tx.CommitAsync(ct);
            return null;
        }

        var payload = await db.TaxInvoiceSnapshots.AsNoTracking().Where(x => x.Id == entry.SnapshotId)
            .Select(x => x.ProtectedPreview).SingleAsync(ct);
        var attempt = Guid.NewGuid();
        await db.TaxDispatches.Where(x => x.Id == entry.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptId, attempt)
                .SetProperty(x => x.LeaseUntilUtc, now + lease)
                .SetProperty(x => x.Status, TaxDispatchStatus.InFlight), ct);
        await tx.CommitAsync(ct);
        return new(entry.Id, attempt, entry.MemoryId, entry.FiscalId, protection.Unprotect(payload));
    }

    public async Task<bool> CompleteAsync(Guid dispatchId, Guid attemptId, TaxDispatchStatus status, CancellationToken ct)
    {
        if (status is not (TaxDispatchStatus.Unknown or TaxDispatchStatus.Accepted or TaxDispatchStatus.Rejected))
            throw new ArgumentOutOfRangeException(nameof(status));

        var affected = await db.TaxDispatches.Where(x => x.Id == dispatchId && x.AttemptId == attemptId &&
                (x.Status == TaxDispatchStatus.InFlight || x.Status == TaxDispatchStatus.Unknown))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
        return affected == 1;
    }
}
