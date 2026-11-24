using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Security;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MappingWorkspaceAccessAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Result != null) return;
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }
        var access = context.HttpContext.RequestServices.GetRequiredService<DocumentAccessPolicy>();
        if (!await access.CanUseMappingWorkspaceAsync(context.HttpContext.RequestAborted))
            context.Result = new ForbidResult();
    }
}
