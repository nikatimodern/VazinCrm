#nullable enable
using System;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

// Customer QC notes support audited edits and soft deletion, independently of item reviews and sales Reports.
public sealed class QualityControlCustomerNote : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Description { get; set; } = string.Empty;
    public QualityControlCustomerNoteSource Source { get; set; } = QualityControlCustomerNoteSource.Manual;
    public Guid? CallRecordingId { get; set; }
    public Guid? LegacyQualityControlId { get; set; }
    // Preserve source speech-to-text independently from the expert-approved note.
    public string? OriginalTranscript { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
