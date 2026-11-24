using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Security;

/// <summary>Preserves a legacy role gate only until its catalog replacement is enabled.
/// The action must independently enforce the exact catalog grant and record scope.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class LegacyCatalogRolesAttribute(string entity, string roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.Result != null) return;
        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var policy = context.HttpContext.RequestServices.GetRequiredService<DocumentAccessPolicy>();
        if (!policy.IsEnforced(entity) &&
            !roles.Split(',').Any(role => principal.IsInRole(role.Trim())))
            context.Result = new ForbidResult();
    }
}
