using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

public interface ITaxSigningBindings
{
    TaxSigningBinding Get(Guid memoryId);
    Task<TaxSigningBinding> GetAsync(Guid memoryId, CancellationToken ct) => Task.FromResult(Get(memoryId));
}

public interface ITaxAuthorityClient
{
    Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct);
    Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct);
    Task<TaxProcessingResult?> InquireAsync(TaxSubmission submission, CancellationToken ct);
    Task<TaxWorkFolderResult?> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct);
}

public sealed class PreparedTaxRequest(Guid attemptId, HttpRequestMessage message, TaxAuthorityProtocol protocol = TaxAuthorityProtocol.V2, Guid? memoryId = null) : IDisposable
{
    public TaxAuthorityProtocol Protocol { get; } = protocol;
    public Guid? MemoryId { get; } = memoryId;
    public Guid AttemptId { get; } = attemptId;
    public HttpRequestMessage Message { get; } = message;
    public void Dispose() => Message.Dispose();
}

public sealed record TaxAcknowledgement(Guid ReferenceNumber, string Evidence);
public sealed record TaxProcessingResult(Guid? ReferenceNumber, string Status, string Evidence);
public sealed record TaxWorkFolderResult(string? InvoiceStatus, string? Article6Status, string? Error, string Evidence);

public sealed class TaxTransportException(string message) : Exception(message);
