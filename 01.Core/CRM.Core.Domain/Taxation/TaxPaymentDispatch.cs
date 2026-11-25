using CRM.Core.Domain.Accountancies.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public enum TaxPaymentStatus { Pending = 0, InFlight = 1, Unknown = 2, Accepted = 3, Rejected = 4 }

public sealed class TaxPaymentDispatch
{
    public Guid Id { get; set; }
    public Guid InvoiceDispatchId { get; set; }
    public Guid AccountancyId { get; set; }
    public decimal Amount { get; set; }
    public string ReferenceHash { get; set; }
    public string ProtectedRequest { get; set; }
    public string ProtectedResponse { get; set; }
    public string CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public TaxPaymentStatus Status { get; set; }
    public Guid? AttemptId { get; set; }
    public DateTimeOffset? AttemptStartedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed record TaxPaymentSource(TaxDispatch Invoice, Accountancy Receipt, string InvoicePayload, decimal CommittedPayments);
public sealed record TaxPaymentRegistration(decimal Amount, string ReferenceHash, string Content);
public sealed record TaxPaymentState(Guid Id, Guid InvoiceDispatchId, Guid AccountancyId, decimal Amount, TaxPaymentStatus Status,
    Guid? AttemptId, DateTimeOffset CreatedAt, DateTimeOffset? AttemptStartedAt, DateTimeOffset? ResolvedAt);

public sealed record PendingTaxPayment(Guid Id, Guid InvoiceDispatchId, string Payload);

public interface ITaxPaymentRepository
{
    Task<Guid> EnqueueAsync(Guid invoiceDispatchId, Guid accountancyId, string actorId,
        Func<TaxPaymentSource, TaxPaymentRegistration> prepare, CancellationToken ct);
    Task<PendingTaxPayment> ReadPendingAsync(Guid id, CancellationToken ct);
    Task<bool> TryStartAsync(Guid id, Guid attempt, DateTimeOffset started, CancellationToken ct);
    Task RecordOutcomeAsync(Guid id, Guid attempt, TaxPaymentStatus status, string evidence, DateTimeOffset now, CancellationToken ct);
    Task<TaxPaymentState> GetStateAsync(Guid id, CancellationToken ct);
}
