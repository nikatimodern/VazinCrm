using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Services;
public sealed class ReleaseNoteAccess(CurrentActor actor)
{
    public async Task<ReleaseNoteReader> ReaderAsync(CancellationToken ct)
    {
        var user = await actor.GetAsync(ct);
        return new(user.User.Id, user.Roles.ToArray(), user.HasRole(AppRoles.Customer), user.Roles.Any(r => r != AppRoles.Customer));
    }
    public async Task DemandAdminAsync(CancellationToken ct)
    {
        var user = await actor.GetAsync(ct);
        if (!user.HasRole(AppRoles.Admin)) throw new UnauthorizedAccessException();
    }
}
