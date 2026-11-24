using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public enum TaxDispatchStatus { Pending, InFlight, Unknown, Accepted, Rejected }

public sealed class TaxDispatch
{
    public Guid Id { get; set; }
    public Guid FactorId { get; set; }
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

public interface ITaxDispatchQueue
{
    Task<TaxDispatchAttempt> ClaimAsync(TimeSpan lease, CancellationToken ct);
    Task<bool> CompleteAsync(Guid dispatchId, Guid attemptId, TaxDispatchStatus status, CancellationToken ct);
}
