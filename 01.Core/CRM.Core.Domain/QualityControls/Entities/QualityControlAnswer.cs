#nullable enable
using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

public sealed class QualityControlAnswer : BaseEntity
{
    public Guid QualityControlId { get; set; }
    public QualityControl QualityControl { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public QualityControlQuestion Question { get; set; } = null!;
    public byte Answer { get; set; }
    public string QuestionTitleSnapshot { get; set; } = string.Empty;
    public string QuestionTextSnapshot { get; set; } = string.Empty;
    public string ZeroAnswerTextSnapshot { get; set; } = string.Empty;
    public string OneAnswerTextSnapshot { get; set; } = string.Empty;
    public int SortOrderSnapshot { get; set; }
}
