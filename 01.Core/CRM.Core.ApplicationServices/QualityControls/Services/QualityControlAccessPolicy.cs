using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.QualityControls.Models;

namespace CRM.Core.ApplicationServices.QualityControls.Services;

public static class QualityControlAccessPolicy
{
    public const string Roles = AppRoles.QcExpert +","+ AppRoles.Admin;

    public static async Task<QualityControlAccess> ForAsync(ApplicationActor actor,
        IDocumentReadAccess permissions, string action, CancellationToken ct, bool requireQuestions = false)
    {
        if (!actor.HasRole(Roles.Split(',')))
            throw new UnauthorizedAccessException();
        if (!await permissions.HasAsync("QualityControl", "View", ct) ||
            (action != "View" && !await permissions.HasAsync("QualityControl", action, ct)) ||
            (requireQuestions && !await permissions.HasAsync("QualityControlQuestion", "View", ct)))
            throw new UnauthorizedAccessException();
        return new(actor.User.Id, actor.User.FullName,true);
    }
}
