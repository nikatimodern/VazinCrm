using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public enum TaxAuthorityEnvironment { Sandbox = 1, Production = 2 }

public enum TaxAuthorityProtocol { V1 = 1, V2 = 2 }

public sealed record TaxSigningBinding(TaxAuthorityEnvironment Environment, string CertificateSha256, string SellerEconomicCode,
    TaxAuthorityProtocol Protocol = TaxAuthorityProtocol.V2, string V1KeyFingerprint = null);
public sealed record TaxSubmissionDraft(Guid DispatchId, Guid MemoryId, string FiscalId, string Snapshot);
public sealed record TaxSubmission(Guid DispatchId, string FiscalId, TaxAuthorityEnvironment Environment,
    string CertificateSha256, string TaxId, string Payload, Guid? AttemptId, DateTimeOffset? StartedAt,
    Guid? ReferenceNumber, TaxDispatchStatus Status, long? InvoiceSerial = null,
    TaxDispatchKind Kind = TaxDispatchKind.Original, TaxSubmissionReference Reference = null,
    TaxAuthorityProtocol Protocol = TaxAuthorityProtocol.V2, string V1KeyFingerprint = null, Guid? MemoryId = null);

public sealed record TaxSubmissionReference(string TaxId, TaxDispatchKind Kind, TaxDispatchStatus Status,
    string WorkFolderEvidence, string EvidenceVersion);

public interface ITaxSubmissionRepository
{
    Task<bool> HasOpenSubmissionsAsync(TaxAuthorityProtocol protocol, CancellationToken ct);
    Task<TaxSubmissionDraft> ReadDraftAsync(Guid dispatchId, CancellationToken ct);
    Task<TaxSubmission> FindAsync(Guid dispatchId, CancellationToken ct);
    Task PinAsync(Guid dispatchId, TaxSigningBinding binding, long serial, string taxId, string payload, CancellationToken ct);
    Task<bool> TryStartAsync(Guid dispatchId, Guid attemptId, DateTimeOffset startedAt, string referenceEvidenceVersion, CancellationToken ct);
    Task RecordAcknowledgementAsync(Guid dispatchId, Guid attemptId, Guid reference, string evidence, CancellationToken ct);
    Task RecordProcessingAsync(Guid dispatchId, Guid attemptId, Guid? reference, string status, string evidence, CancellationToken ct);
    Task RecordWorkFolderAsync(Guid dispatchId, Guid attemptId, string evidence, CancellationToken ct);
    Task RecordInquiryWithoutResultAsync(Guid dispatchId, Guid attemptId, TaxDispatchEventKind kind, CancellationToken ct);
}
