#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace CRM.Infrastructures.Sql.AppUsers.Repositories;

public sealed class AccountUserRepository(UserManager<AppUser> users, UserContextDb db) : IAccountUserRepository
{
    public async Task<AppUser?> FindByIdAsync(string? id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return id is null ? null : await users.FindByIdAsync(id);
    }
    public async Task<AppUser?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await users.FindByNameAsync(name);
    }
    public Task<List<AppUser>> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        users.Users.Where(u => u.PhoneNumber == phone).ToListAsync(cancellationToken);
    public Task<List<AppUser>> FindByUserNameAsync(string name, CancellationToken cancellationToken) =>
        users.Users.Where(u => u.UserName == name).ToListAsync(cancellationToken);
    public Task<List<AppUser>> GetEmployeesAsync(CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().Where(u => u.IsEmployee).ToListAsync(cancellationToken);
    public async Task<List<AppUser>> GetInRoleAsync(string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await users.GetUsersInRoleAsync(role)).ToList();
    }
    public async Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await users.GetRolesAsync(user);
    }
    public async Task<AccountIdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Map(await users.CreateAsync(user));
    }
    public async Task AddToRoleAsync(AppUser user, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await users.AddToRoleAsync(user, role);
    }
    public async Task<AccountIdentityResult> ResetPasswordAsync(AppUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        return Map(await users.ResetPasswordAsync(user, token, password));
    }
    public async Task<AccountIdentityResult> UpdateLockoutAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await users.UpdateSecurityStampAsync(user);
        return Map(await users.UpdateAsync(user));
    }
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
    private static AccountIdentityResult Map(IdentityResult result) =>
        new(result.Succeeded, result.Errors.Select(e => new AccountIdentityError(e.Code, e.Description)).ToList());
}
