#nullable enable
using CRM.Framework.Domain;

namespace CRM.Core.Domain.QualityControls.Entities;

public sealed class QualityControlQuestion : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string ZeroAnswerText { get; set; } = string.Empty;
    public string OneAnswerText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
