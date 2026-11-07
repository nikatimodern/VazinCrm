#nullable enable
using System;
using System.Collections.Generic;
namespace CRM.Core.Domain.ReleaseNotes.Entities;
public enum ReleaseNoteStatus { Draft = 0, Published = 1, Archived = 2 }
public enum ReleaseNoteAudience { All = 0, InternalUsers = 1, Customers = 2, SelectedRoles = 3 }
public enum ReleaseNoteItemType { NewFeature = 1, Improvement = 2, BugFix = 3 }
public sealed class ReleaseNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public ReleaseNoteStatus Status { get; set; } = ReleaseNoteStatus.Draft;
    public ReleaseNoteAudience Audience { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? PublishDate { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public bool IsImportant { get; set; }
    public bool ShowPopup { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public List<ReleaseNoteItem> Items { get; set; } = [];
    public List<UserReleaseNote> UserViews { get; set; } = [];
    public List<ReleaseNoteRole> Roles { get; set; } = [];
}
public sealed class ReleaseNoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReleaseNoteId { get; set; }
    public ReleaseNote ReleaseNote { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public ReleaseNoteItemType Type { get; set; }
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
    public string? LearnMoreUrl { get; set; }
}
public sealed class UserReleaseNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public Guid ReleaseNoteId { get; set; }
    public ReleaseNote ReleaseNote { get; set; } = null!;
    public DateTime SeenDate { get; set; } = DateTime.UtcNow;
}
public sealed class ReleaseNoteRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReleaseNoteId { get; set; }
    public ReleaseNote ReleaseNote { get; set; } = null!;
    public string RoleName { get; set; } = "";
}
