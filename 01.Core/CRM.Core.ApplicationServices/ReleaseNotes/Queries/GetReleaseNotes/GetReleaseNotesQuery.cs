using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNotes;
public sealed record GetReleaseNotesQuery(string? Search, ReleaseNoteStatus? Status) : IQuery<List<ReleaseNoteData>>;
public sealed class GetReleaseNotesQueryHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : IQueryHandler<GetReleaseNotesQuery, List<ReleaseNoteData>>
{
    public async Task<List<ReleaseNoteData>> HandleAsync(GetReleaseNotesQuery query, CancellationToken ct) { await access.DemandAdminAsync(ct); return await repository.GetAdminListAsync(query.Search, query.Status, ct); }
}
