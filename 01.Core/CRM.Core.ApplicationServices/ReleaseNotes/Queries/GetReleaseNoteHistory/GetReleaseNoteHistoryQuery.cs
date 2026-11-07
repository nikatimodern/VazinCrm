using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteHistory;
public sealed record GetReleaseNoteHistoryQuery : IQuery<List<ReleaseNoteData>>;
public sealed class GetReleaseNoteHistoryQueryHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : IQueryHandler<GetReleaseNoteHistoryQuery, List<ReleaseNoteData>>
{
    public async Task<List<ReleaseNoteData>> HandleAsync(GetReleaseNoteHistoryQuery query, CancellationToken ct) =>
        await repository.GetVisibleAsync(await access.ReaderAsync(ct), false, DateTime.UtcNow, ct);
}
