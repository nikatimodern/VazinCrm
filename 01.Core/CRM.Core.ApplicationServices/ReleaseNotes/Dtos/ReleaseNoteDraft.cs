using CRM.Core.Domain.ReleaseNotes.Entities;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
public sealed class ReleaseNoteDraft
{
    public string Version { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset? PublishDate { get; set; }
    public bool IsImportant { get; set; }
    public bool ShowPopup { get; set; } = true;
    public ReleaseNoteAudience Audience { get; set; }
    public string RowVersion { get; set; } = "";
    public List<string> Roles { get; set; } = [];
    public List<ReleaseNoteItemDraft> Items { get; set; } = [];
}
public sealed class ReleaseNoteItemDraft
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public ReleaseNoteItemType Type { get; set; } = ReleaseNoteItemType.Improvement;
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
    public string? LearnMoreUrl { get; set; }
}
public sealed record ReleaseNoteResult(int Status, string? Message = null, Guid? Id = null);
public sealed record ReleaseNoteAdminPage(List<string> Roles);
