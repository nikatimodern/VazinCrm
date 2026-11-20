#nullable enable
using System;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

// Append-only contact outcome events. Removal is soft-deletion with an audit trail.
// Old aggregate counters are represented by a clearly marked legacy entry.
public sealed class QualityControlContactHistory : BaseEntity
{
    public Guid CustomerId { get; set; }
    public QualityControlCustomerContactStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public bool IsLegacy { get; set; }
    public string? MobileAtAttempt { get; set; }
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
    public string? DeletedByName { get; set; }
    public string? DeleteReason { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoredById { get; set; }
    public string? RestoredByName { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
