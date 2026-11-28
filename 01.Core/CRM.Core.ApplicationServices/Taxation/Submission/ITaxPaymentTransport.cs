using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

public interface ITaxPaymentTransport
{
    Task<PreparedTaxRequest> PreparePaymentAsync(TaxSubmission invoice, Guid attemptId, string payload, CancellationToken ct);
    Task<TaxPaymentReply> SubmitPaymentAsync(PreparedTaxRequest request, CancellationToken ct);
}

public sealed record TaxPaymentReply(TaxPaymentStatus Status, string Evidence);
