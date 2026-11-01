using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.Domain.AppUsers.Entities;
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
public sealed class CustomerAccess(IAccountUserRepository users, IAccountRequestContext context)
{
    public async Task<CustomerIdentity> GetAsync(CancellationToken ct)
    { var user = await users.FindByIdAsync(context.UserId, ct) ?? throw new UnauthorizedAccessException(); return new(user, await users.GetRolesAsync(user, ct)); }
    public static void Stamp(Customer customer, AppUser user)
    { customer.UserModify = user.FullName; customer.UserModifyID = user.Id; customer.ModifyDate = DateTime.Now; customer.ModifyDateTxt = BaseServices.GetDateTimePersian(); }
}
