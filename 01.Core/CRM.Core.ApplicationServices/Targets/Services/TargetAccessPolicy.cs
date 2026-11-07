using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;

namespace CRM.Core.ApplicationServices.Targets.Services;

public static class TargetAccessPolicy
{
    public const string Roles = AppRoles.Admin + "," + AppRoles.SalesAdmin;

    public static string RequireManager(ApplicationActor actor)
    {
        if (!actor.HasRole(AppRoles.Admin, AppRoles.SalesAdmin))
            throw new UnauthorizedAccessException();
        return actor.User.Id;
    }
}
