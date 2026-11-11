using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.AppUsers.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteAdminPage;
public sealed record GetReleaseNoteAdminPageQuery : IQuery<ReleaseNoteAdminPage>;
public sealed class GetReleaseNoteAdminPageQueryHandler(ReleaseNoteAccess access, IAdminUserRepository users) : IQueryHandler<GetReleaseNoteAdminPageQuery, ReleaseNoteAdminPage>
{
    public async Task<ReleaseNoteAdminPage> HandleAsync(GetReleaseNoteAdminPageQuery query, CancellationToken ct) { await access.DemandAdminAsync(ct); return new((await users.GetRolesAsync(ct)).Select(x => x.Name).ToList()); }
}
