#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.AppUsers.Repositories;
public sealed class AdminUserRepository(UserContextDb db, UserManager<AppUser> users, RoleManager<IdentityRole> roles, IUserValidator<AppUser> userValidator, IPasswordValidator<AppUser> passwordValidator, IPasswordHasher<AppUser> hasher) : IAdminUserRepository
{
    private static AccountIdentityResult Map(IdentityResult result) => new(result.Succeeded, result.Errors.Select(e => new AccountIdentityError(e.Code, e.Description)).ToList());
    public Task<List<AdminRole>> GetRolesAsync(CancellationToken ct) => roles.Roles.AsNoTracking().Select(r => new AdminRole(r.Id, r.Name ?? "")).ToListAsync(ct);
    public async Task<AdminUserPage> GetPageAsync(AdminUserFilter filter, CancellationToken ct)
    {
        var query = users.Users.AsNoTracking().Where(u => !u.OrganAdmin);
        // This filter is specific to access administration. The independent user-kind
        // flags may overlap, so a user can appear in several filtered lists.
        // Existing employee/non-employee filters remain unchanged for other pages.
        if (filter.UserType is not null)
        {
            query = filter.UserType switch
            {
                "employee" => query.Where(u => u.IsEmployee),
                "customer" => query.Where(u => u.IsCustomer),
                "supplier" => query.Where(u => u.IsSupplier),
                _ => query
            };
        }
        else if (filter.EmployeeType == "employee")
            query = query.Where(u => u.IsEmployee);
        else if (filter.EmployeeType == "nonEmployee")
            query = query.Where(u => !u.IsEmployee);
        if (filter.AccountStatus == "active")
            query = query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= filter.Now);
        else if (filter.AccountStatus == "blocked")
            query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > filter.Now);
        if (filter.Roles.Count > 0)
        {
            var names = filter.Roles.Select(roles.NormalizeKey).ToArray();
            var roleIds = db.Roles.Where(r => names.Contains(r.NormalizedName)).Select(r => r.Id);
            query = query.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(u => (u.UserName != null && u.UserName.Contains(term)) || (u.FirstName != null && u.FirstName.Contains(term)) || (u.LastName != null && u.LastName.Contains(term)) || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) || (u.Email != null && u.Email.Contains(term)) || (u.InternalTels != null && u.InternalTels.Contains(term)));
        }

        var count = await query.CountAsync(ct);
        var all = filter.PageSize <= 0;
        var size = all ? Math.Max(count, 1) : Math.Clamp(filter.PageSize, 5, 50);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)size));
        var page = Math.Clamp(filter.Page, 1, pages);
        var data = await query.OrderByDescending(u => u.CreateDate).ThenByDescending(u => u.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var ids = data.Select(u => u.Id).ToArray();
        var memberships = await (
            from ur in db.UserRoles.AsNoTracking()join role in db.Roles.AsNoTracking()on ur.RoleId equals role.Id
            where ids.Contains(ur.UserId)select new
            {
                ur.UserId,
                role.Name
            }

        ).ToListAsync(ct);
        var lookup = memberships.ToLookup(m => m.UserId, m => m.Name ?? "");
        foreach (var user in data)
            user.RolsUsers = lookup[user.Id].ToList();
        return new(data, count, page, all ? 0 : size, pages);
    }

    public async Task<AppUser?> FindByIdAsync(string id, bool tracking, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return tracking ? await users.FindByIdAsync(id) : await users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<AppUser?> FindByNameAsync(string name, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await users.FindByNameAsync(name);
    }

    public async Task<IList<string>> GetUserRolesAsync(AppUser user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await users.GetRolesAsync(user);
    }

    public async Task<bool> RoleExistsAsync(string role, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await roles.RoleExistsAsync(role);
    }

    public async Task<AccountIdentityResult> CreateAsync(AppUser user, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await users.CreateAsync(user, password));
    }

    public async Task<AccountIdentityResult> UpdateAsync(AppUser user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await users.UpdateAsync(user));
    }

    public async Task<AccountIdentityResult> DeleteAsync(AppUser user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await users.DeleteAsync(user));
    }

    public async Task<AccountIdentityResult> ValidateUserAsync(AppUser user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await userValidator.ValidateAsync(users, user));
    }

    public async Task<AccountIdentityResult> ValidatePasswordAsync(AppUser user, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await passwordValidator.ValidateAsync(users, user, password));
    }

    public string HashPassword(AppUser user, string password) => hasher.HashPassword(user, password);
    public async Task<AccountIdentityResult> AddRolesAsync(AppUser user, IEnumerable<string> values, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await users.AddToRolesAsync(user, values));
    }

    public async Task<AccountIdentityResult> RemoveRolesAsync(AppUser user, IEnumerable<string> values, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Map(await users.RemoveFromRolesAsync(user, values));
    }

    public async Task<AdminRoleUpdateResult> ReplaceRolesAsync(string userName, IReadOnlyCollection<string> selectedRoles, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            ct.ThrowIfCancellationRequested();
            // This dedicated mutation must reload Identity state after a rolled-back attempt.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var user = await users.FindByNameAsync(userName);
            if (user == null)
                return new AdminRoleUpdateResult(false, null, Array.Empty<string>());

            var current = await users.GetRolesAsync(user);
            var remove = current.Except(selectedRoles).ToArray();
            var add = selectedRoles.Except(current).ToArray();
            if (remove.Length > 0)
            {
                var result = await users.RemoveFromRolesAsync(user, remove);
                if (!result.Succeeded)
                    return new AdminRoleUpdateResult(true, Map(result), Array.Empty<string>());
            }
            if (add.Length > 0)
            {
                var result = await users.AddToRolesAsync(user, add);
                if (!result.Succeeded)
                    return new AdminRoleUpdateResult(true, Map(result), Array.Empty<string>());
            }
            var updated = await users.GetRolesAsync(user);
            await transaction.CommitAsync(ct);
            return new AdminRoleUpdateResult(true, null, updated);
        });
    }
}
