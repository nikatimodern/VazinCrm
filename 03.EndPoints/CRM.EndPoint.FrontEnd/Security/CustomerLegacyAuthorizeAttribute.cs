using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Security;

// Preserve legacy role rules when scoped access is not enabled. Never changes
// the policies for any controller outside the person directory.
public enum CustomerLegacyMode { NotCustomerOnly, Managers, Admin }

[AttributeUsage(AttributeTargets.Method)]
public sealed class CustomerLegacyAuthorizeAttribute(CustomerLegacyMode mode) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (config.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions")) return;
        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        var permitted = mode switch
        {
            CustomerLegacyMode.NotCustomerOnly => !principal.IsInRole("مشتری"),
            CustomerLegacyMode.Managers =>
                principal.IsInRole("مدیر ارشد") || principal.IsInRole("مدیر فروش"),
            CustomerLegacyMode.Admin => principal.IsInRole("مدیر ارشد"),
            _ => false
        };
        if (!permitted) context.Result = new ForbidResult();
    }
}
