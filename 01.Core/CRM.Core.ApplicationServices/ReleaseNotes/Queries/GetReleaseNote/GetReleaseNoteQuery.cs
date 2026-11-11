using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNote;
public sealed record GetReleaseNoteQuery(Guid Id) : IQuery<ReleaseNoteData?>;
public sealed class GetReleaseNoteQueryHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : IQueryHandler<GetReleaseNoteQuery, ReleaseNoteData?>
{
    public async Task<ReleaseNoteData?> HandleAsync(GetReleaseNoteQuery query, CancellationToken ct) { await access.DemandAdminAsync(ct); return await repository.GetAsync(query.Id, ct); }
}
