using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.QualityControls.Models;

namespace CRM.Core.ApplicationServices.QualityControls.Services;

public static class QualityControlAccessPolicy
{
    public const string Roles = AppRoles.QcExpert +","+ AppRoles.Admin;

    public static QualityControlAccess For(ApplicationActor actor)
    {
        if (!actor.HasRole(Roles.Split(',')))
            throw new UnauthorizedAccessException();
        return new(actor.User.Id, actor.User.FullName,true);
    }
}
