using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Security;

/// <summary>
/// An additional catalog gate for operations that have no meaningful per-record scope.
/// Existing authentication, role checks and relationship constraints remain mandatory.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CatalogPermissionAttribute(string entity, string action = "View") : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Result != null) return;
        var http = context.HttpContext;
        var policy = http.RequestServices.GetRequiredService<DocumentAccessPolicy>();
        if (!policy.IsEnforced(entity)) return;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var ct = http.RequestAborted;
        if (!await policy.HasAsync(entity, "View", ct) ||
            (action != "View" && !await policy.HasAsync(entity, action, ct)))
            context.Result = new ForbidResult();
    }
}
