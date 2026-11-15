#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.AccessCatalog.Repositories;

public sealed record AccessHistoryItem(Guid Id, DateTime CreatedAtUtc, string Kind,
    string Title, string ActorName, int ChangeCount);
public sealed record AccessHistoryPage(int Page, int Total, int TotalPages, IReadOnlyList<AccessHistoryItem> Items);
public sealed record AccessHistoryChange(string Key, string Kind, string Title, string Before, string After);
public sealed record AccessHistoryDetail(Guid Id, string Kind, string Title, DateTime CreatedAtUtc,
    string ActorName, int ChangeCount, IReadOnlyList<AccessHistoryChange> Changes);
public sealed record AccessHistoryPreview(Guid Id, string Mode, string Revision, int ConflictCount,
    IReadOnlyList<AccessHistoryChange> Changes, IReadOnlyList<string> Warnings);
public sealed record AccessHistoryResult(int Status, string Message);

public interface IAccessHistoryService
{
    Task<Guid> BeginAsync(string action, string? actorId, CancellationToken ct);
    Task FinishAsync(Guid id, bool succeeded, CancellationToken ct);
    Task<AccessHistoryResult> CreateManualAsync(string title, string? actorId, CancellationToken ct);
    Task<AccessHistoryPage> ListAsync(int page, string? kind, string? search, CancellationToken ct);
    Task<AccessHistoryDetail?> DetailAsync(Guid id, CancellationToken ct);
    Task<AccessHistoryPreview?> PreviewAsync(Guid id, string mode, CancellationToken ct);
    Task<AccessHistoryResult> RestoreAsync(Guid id, string mode, string revision, string? actorId, CancellationToken ct);
}
