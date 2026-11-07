using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetUnseenReleaseNotes;
public sealed record GetUnseenReleaseNotesQuery : IQuery<List<ReleaseNoteData>>;
public sealed class GetUnseenReleaseNotesQueryHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : IQueryHandler<GetUnseenReleaseNotesQuery, List<ReleaseNoteData>>
{
    public async Task<List<ReleaseNoteData>> HandleAsync(GetUnseenReleaseNotesQuery query, CancellationToken ct) =>
        await repository.GetVisibleAsync(await access.ReaderAsync(ct), true, DateTime.UtcNow, ct);
}
