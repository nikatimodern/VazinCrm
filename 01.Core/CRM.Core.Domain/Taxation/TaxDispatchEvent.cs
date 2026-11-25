using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Taxation;

public enum TaxDispatchEventKind { SubmissionStarted = 1, Acknowledgement, Processing, WorkFolder, Unknown }

public sealed class TaxDispatchEvent
{
    public long Id { get; set; }
    public Guid DispatchId { get; set; }
    public Guid AttemptId { get; set; }
    public TaxDispatchEventKind Kind { get; set; }
    public string Result { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed record TaxDispatchDetails(Guid Id, Guid SnapshotId, Guid MemoryId, TaxDispatchKind Kind,
    TaxDispatchStatus Status, string TaxId, Guid? AttemptId, Guid? ReferenceNumber, bool HasAcknowledgement,
    DateTime QueuedUtc, DateTimeOffset? StartedAt, string ProcessingStatus, bool HasWorkFolderEvidence,
    string WorkFolderStatus, string Article6Status,
    IReadOnlyList<TaxDispatchEvent> History, long? OlderHistoryBefore = null, TaxAuthorityProtocol Protocol = TaxAuthorityProtocol.V2);
