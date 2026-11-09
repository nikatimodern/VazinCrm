#nullable enable
using System;
using System.Collections.Generic;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

public sealed class QualityControl : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public string? Description { get; set; }
    // Nullable until legacy rows are populated by a separate, explicitly scheduled backfill.
    public QualityControlOutcome? Status { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<QualityControlAnswer> Answers { get; set; } = new List<QualityControlAnswer>();
}
