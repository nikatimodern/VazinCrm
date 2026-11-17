#nullable enable
using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

// Snapshots protect historic answers if question wording changes later.
public sealed class QualityControlItemAnswer : BaseEntity
{
    public Guid QualityControlItemReviewId { get; set; }
    public QualityControlItemReview QualityControlItemReview { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public QualityControlQuestion Question { get; set; } = null!;
    public byte Answer { get; set; }
    public string QuestionTitleSnapshot { get; set; } = string.Empty;
    public string QuestionTextSnapshot { get; set; } = string.Empty;
    public string ZeroAnswerTextSnapshot { get; set; } = string.Empty;
    public string OneAnswerTextSnapshot { get; set; } = string.Empty;
    public int SortOrderSnapshot { get; set; }
}
