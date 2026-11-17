#nullable enable
using System;
using System.Collections.Generic;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

// One independent review per issued sales-invoice line, never per product name.
public sealed class QualityControlItemReview : BaseEntity
{
    public Guid FactorItemId { get; set; }
    public FactorItem FactorItem { get; set; } = null!;
    public QualityControlOutcome Status { get; set; }
    public string? Description { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<QualityControlItemAnswer> Answers { get; set; } = new List<QualityControlItemAnswer>();
}
