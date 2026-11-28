using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.HumanResources.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CRM.EndPoint.FrontEnd.Services.HumanResources;

// Temporary user-approved senior administrator policy until AccLevel is integrated.
// Authorization is resolved on the server for each operation, never from request data.
public sealed class AdminHrEmployeeAccess(IHttpContextAccessor httpContext,
    UserManager<AppUser> userManager, IHrAccessScopeRepository scope) : IHrEmployeeAccess
{
    public async Task<HrEmployeeAccess> GetAsync(CancellationToken ct)
    {
        var principal = httpContext.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || !principal.IsInRole(AppRoles.Admin))
            return HrEmployeeAccess.Denied;

        ct.ThrowIfCancellationRequested();
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !await userManager.IsInRoleAsync(user, AppRoles.Admin))
            return HrEmployeeAccess.Denied;

        var employees = await scope.GetEmployeeIdsAsync(ct);
        var accounts = await scope.GetEmployeeAccountIdsAsync(ct);
        return new HrEmployeeAccess(user.Id, true, true,
            employees, employees, employees, accounts,
            employees, employees, employees, employees,
            true, employees, employees);
    }
}
