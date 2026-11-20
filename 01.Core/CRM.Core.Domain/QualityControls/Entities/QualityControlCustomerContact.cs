#nullable enable
using System;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

// Customer-level contact outcome is independent of QC results on invoice items.
public sealed class QualityControlCustomerContact : BaseEntity
{
    public Guid CustomerId { get; set; }
    public QualityControlCustomerContactStatus Status { get; set; }
    // No-response hides the customer only for the calendar day of this attempt.
    public DateTime? LastNoResponseAt { get; set; }
    public int NoResponseAttempts { get; set; }
    // Re-enable the customer when the registered mobile number changes.
    public string? InvalidMobileAtMark { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
