using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public enum TaxDispatchStatus { Pending = 0, InFlight = 1, Unknown = 2, Accepted = 3, Rejected = 4 }

public enum TaxDispatchKind { Original = 0, Correction = 1, Cancellation = 2 }

public sealed class TaxDispatch
{
    public Guid Id { get; set; }
    public Guid FactorId { get; set; }
    public TaxDispatchKind Kind { get; set; }
    public Guid? ReferencedDispatchId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid MemoryId { get; set; }
    public string FiscalId { get; set; }
    public string MemoryTitle { get; set; }
    public DateTime MemoryUpdatedUtc { get; set; }
    public bool MemoryWasActive { get; set; }
    public DateTime QueuedUtc { get; set; }
    public TaxDispatchStatus Status { get; set; }
    public Guid? AttemptId { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
}

public sealed record TaxDispatchAttempt(Guid DispatchId, Guid AttemptId, Guid MemoryId, string FiscalId, string Preview);

public sealed record TaxDispatchState(Guid DispatchId, Guid? AttemptId, TaxDispatchStatus Status);

public interface ITaxDispatchQueue
{
    Task<TaxDispatchAttempt> ClaimAsync(Guid dispatchId, TimeSpan lease, CancellationToken ct);
    Task<TaxDispatchState> GetStateAsync(Guid dispatchId, CancellationToken ct);
    // Accepted and Rejected require a definitive outcome from the tax authority.
    // Unknown is eligible for inquiry and cannot be claimed again.
    Task<bool> CompleteAsync(Guid dispatchId, Guid attemptId, TaxDispatchStatus status, CancellationToken ct);
}
