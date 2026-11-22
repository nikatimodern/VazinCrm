using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Extensions.Configuration;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.Customers.Entities;
using CRM.Framework;
namespace CRM.Core.ApplicationServices.Customers.Services;

public sealed record CustomerIdentity(AppUser User, IList<string> Roles)
{
    public bool IsAdmin => Roles.Contains("مدیر ارشد");
    public bool CanManage => IsAdmin || Roles.Contains("مدیر فروش");
    public bool IsCustomer => Roles.Contains("مشتری");
}
public sealed class CustomerAccess(
    IAccountUserRepository users, IAccountRequestContext context,
    IAccessScopeService scopeService, IConfiguration settings)
{
    public bool ScopeEnforced => bool.TryParse(settings["AccessCatalog:EnforceCustomerPermissions"], out var enabled) && enabled;

    public Task<byte> GetScopeAsync(CustomerIdentity identity, string permission, CancellationToken ct) =>
        scopeService.GetEffectiveScopeAsync(identity.User.Id, "Customer." + permission, ct);

    public Task<bool> CanAccessAsync(CustomerIdentity identity, string permission, Guid customerId, CancellationToken ct) =>
        !ScopeEnforced ? Task.FromResult(true) :
        scopeService.CanAccessCustomerAsync(identity.User.Id, "Customer." + permission, customerId, ct);

    public async Task<bool> CanAccessAllAsync(CustomerIdentity identity, string permission, CancellationToken ct) =>
        !ScopeEnforced || await GetScopeAsync(identity, permission, ct) == AccessScopes.All;

    public async Task<CustomerIdentity> GetAsync(CancellationToken ct)
    { var user = await users.FindByIdAsync(context.UserId, ct) ?? throw new UnauthorizedAccessException(); return new(user, await users.GetRolesAsync(user, ct)); }
    public static void Stamp(Customer customer, AppUser user)
    { customer.UserModify = user.FullName; customer.UserModifyID = user.Id; customer.ModifyDate = DateTime.Now; customer.ModifyDateTxt = BaseServices.GetDateTimePersian(); }
}
