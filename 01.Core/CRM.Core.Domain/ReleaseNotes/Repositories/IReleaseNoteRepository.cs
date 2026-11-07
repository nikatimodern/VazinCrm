#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ReleaseNotes.Entities;
namespace CRM.Core.Domain.ReleaseNotes.Repositories;
public sealed record ReleaseNoteReader(string UserId, string[] Roles, bool IsCustomer, bool IsInternal);
public enum ReleaseNoteMutation { Success, NotFound, Conflict, Ineligible }
public sealed record ReleaseNoteItemData(Guid Id, string Title, string? Description, ReleaseNoteItemType Type, int SortOrder, string? ImageUrl, string? LearnMoreUrl);
public sealed record ReleaseNoteData(Guid Id, string Version, string Title, string? Description, ReleaseNoteStatus Status, ReleaseNoteAudience Audience, DateTime CreateDate, DateTime? PublishDate, bool IsImportant, bool ShowPopup, string RowVersion, List<string> Roles, List<ReleaseNoteItemData> Items);
public interface IReleaseNoteRepository
{
    Task<List<ReleaseNoteData>> GetAdminListAsync(string? search, ReleaseNoteStatus? status, CancellationToken ct);
    Task<ReleaseNoteData?> GetAsync(Guid id, CancellationToken ct);
    Task<List<ReleaseNoteData>> GetVisibleAsync(ReleaseNoteReader reader, bool unseenOnly, DateTime now, CancellationToken ct);
    Task<string?> GetLatestVisibleVersionAsync(ReleaseNoteReader reader, DateTime now, CancellationToken ct);
    Task<(ReleaseNoteMutation Result, Guid Id)> SaveDraftAsync(ReleaseNote draft, Guid? id, byte[]? expectedVersion, CancellationToken ct);
    Task<ReleaseNoteMutation> ChangeStatusAsync(Guid id, ReleaseNoteStatus target, DateTime now, CancellationToken ct);
    Task<ReleaseNoteMutation> DeleteDraftAsync(Guid id, CancellationToken ct);
    Task<ReleaseNoteMutation> AcknowledgeAsync(ReleaseNoteReader reader, Guid[] ids, DateTime now, CancellationToken ct);
}
