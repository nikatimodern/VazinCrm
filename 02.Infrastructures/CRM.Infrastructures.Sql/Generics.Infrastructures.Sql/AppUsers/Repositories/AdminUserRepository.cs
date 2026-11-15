#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using CRM.Core.Domain.AccessCatalog.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;

namespace CRM.Infrastructures.Sql.AppUsers.Repositories;
public sealed class AdminUserRepository(UserContextDb db, UserManager<AppUser> users, RoleManager<IdentityRole> roles, IUserValidator<AppUser> userValidator, IPasswordValidator<AppUser> passwordValidator, IPasswordHasher<AppUser> hasher) : IAdminUserRepository
{
    private static AccountIdentityResult Map(IdentityResult result) => new(result.Succeeded, result.Errors.Select(e => new AccountIdentityError(e.Code, e.Description)).ToList());
    public Task<List<AdminRole>> GetRolesAsync(CancellationToken ct) => roles.Roles.AsNoTracking().Select(r => new AdminRole(r.Id, r.Name ?? "")).ToListAsync(ct);

    public async Task<IReadOnlyList<AdminPosition>> GetPositionsAsync(CancellationToken ct) =>
        await db.BaseAdmins.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.PersianTitle)
            .Select(x => new AdminPosition(x.Id, x.PersianTitle ?? x.LatinTitle ?? "بدون عنوان"))
            .ToListAsync(ct);

    public async Task<AdminUserProfile?> GetUserProfileAsync(string userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == userId && !x.OrganAdmin, ct);
        if (user is null) return null;
        var userRoles = await db.UserRoles.AsNoTracking()
            .Where(x => x.UserId == userId).Select(x => x.RoleId).ToArrayAsync(ct);
        return new AdminUserProfile(
            user.Id, user.UserName ?? "", user.FirstName, user.LastName, user.PhoneNumber, user.Email,
            user.InternalTels, user.IsEmployee, user.IsCustomer, user.IsSupplier,
            user.PositionId, user.PositionTxt, userRoles, user.ConcurrencyStamp);
    }

    private static AdminUserProfileWriteResult ProfileFailure(IdentityResult result)
    {
        var duplicate = result.Errors.Any(e => e.Code.StartsWith("Duplicate", StringComparison.Ordinal));
        return new(duplicate ? 409 : 400,
            duplicate ? "نام کاربری یا ایمیل قبلاً ثبت شده است." :
            string.Join("؛ ", result.Errors.Select(e => e.Description)));
    }

    public async Task<AdminUserProfileWriteResult> SaveUserProfileAsync(
        AdminUserProfileInput input, string? actorId, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                var isCreate = string.IsNullOrWhiteSpace(input.UserId);
                AppUser? user = isCreate ? null : await users.FindByIdAsync(input.UserId!);
                if (!isCreate && (user is null || user.OrganAdmin))
                    return new AdminUserProfileWriteResult(404, "کاربر پیدا نشد.");

                if (!isCreate && !string.Equals(user!.ConcurrencyStamp, input.ExpectedConcurrencyStamp, StringComparison.Ordinal))
                    return new AdminUserProfileWriteResult(409, "اطلاعات کاربر هم‌زمان تغییر کرده است. فرم را دوباره باز کنید.");

                var chosenIds = input.RoleIds.ToHashSet(StringComparer.Ordinal);
                var chosenRoles = await db.Roles.AsNoTracking()
                    .Where(x => chosenIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
                if (chosenRoles.Count != chosenIds.Count ||
                    chosenRoles.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                    return new AdminUserProfileWriteResult(409, "فهرست نقش‌ها تغییر کرده است. دوباره تلاش کنید.");
                var desiredRoles = chosenRoles.Select(x => x.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var adminRole = await db.Roles.AsNoTracking()
                    .Where(x => x.Name == "مدیر ارشد").Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);

                if (!isCreate && adminRole is not null)
                {
                    var currentlyAdmin = await db.UserRoles.AsNoTracking()
                        .AnyAsync(x => x.UserId == input.UserId && x.RoleId == adminRole.Id, ct);
                    if (currentlyAdmin && !chosenIds.Contains(adminRole.Id))
                    {
                        if (string.Equals(actorId, input.UserId, StringComparison.Ordinal))
                            return new AdminUserProfileWriteResult(409, "نمی‌توانید نقش مدیر ارشد را از حساب خودتان حذف کنید.");
                        var remaining = await db.UserRoles.AsNoTracking()
                            .CountAsync(x => x.RoleId == adminRole.Id, ct);
                        if (remaining <= 1)
                            return new AdminUserProfileWriteResult(409, "امکان حذف نقش آخرین مدیر ارشد سیستم وجود ندارد.");
                    }
                }

                // Keep an existing archived position editable: do not force a user to
                // lose their historical assignment just to update their name or phone.
                // New assignments must always point to an active position.
                var position = input.PositionId.HasValue
                    ? await db.BaseAdmins.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == input.PositionId.Value &&
                            (!x.IsDeleted || (!isCreate && user!.PositionId == x.Id)), ct)
                    : null;
                if (input.PositionId.HasValue && position is null)
                    return new AdminUserProfileWriteResult(400, "سمت کاری انتخاب‌شده معتبر نیست.");

                user ??= new AppUser();
                user.UserName = input.UserName;
                user.FirstName = input.FirstName;
                user.LastName = input.LastName;
                user.PhoneNumber = input.PhoneNumber;
                user.Email = input.Email;
                user.InternalTels = input.InternalTels;
                user.IsEmployee = input.IsEmployee;
                user.IsCustomer = input.IsCustomer;
                user.IsSupplier = input.IsSupplier;
                user.PositionId = input.PositionId;
                user.PositionTxt = position?.PersianTitle ?? position?.LatinTitle;

                if (isCreate)
                {
                    var created = await users.CreateAsync(user, input.Password!);
                    if (!created.Succeeded) return ProfileFailure(created);
                }
                else
                {
                    if (input.Password is not null)
                    {
                        var passwordValidation = await passwordValidator.ValidateAsync(users, user, input.Password);
                        if (!passwordValidation.Succeeded) return ProfileFailure(passwordValidation);
                        user.PasswordHash = hasher.HashPassword(user, input.Password);
                        user.SecurityStamp = Guid.NewGuid().ToString("N");
                    }
                    var updated = await users.UpdateAsync(user);
                    if (!updated.Succeeded) return ProfileFailure(updated);
                }

                var currentRoles = isCreate ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : (await users.GetRolesAsync(user)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var remove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
                var add = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
                if (remove.Length > 0)
                {
                    var removed = await users.RemoveFromRolesAsync(user, remove);
                    if (!removed.Succeeded) return ProfileFailure(removed);
                }
                if (add.Length > 0)
                {
                    var added = await users.AddToRolesAsync(user, add);
                    if (!added.Succeeded) return ProfileFailure(added);
                }

                await transaction.CommitAsync(ct);
                return new AdminUserProfileWriteResult(200, UserId: user.Id);
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "کاربر هم‌زمان تغییر کرده است. فرم را دوباره باز کنید.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 547 or 2601 or 2627)
        {
            return new(409, "اطلاعات کاربر یا نقش‌ها هم‌زمان تغییر کرده است.");
        }
    }

    private static AdminRoleWriteResult IdentityError(IdentityResult result)
    {
        if (result.Succeeded) return new(200);
        var duplicate = result.Errors.Any(e => e.Code == "DuplicateRoleName");
        return new(duplicate ? 409 : 400,
            duplicate ? "نقشی با همین نام قبلاً ثبت شده است." :
            string.Join("؛ ", result.Errors.Select(e => e.Description)));
    }

    public async Task<AdminRoleWriteResult> CreateRoleAsync(string name, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var role = new IdentityRole(name);
            var created = await roles.CreateAsync(role);
            return created.Succeeded ? new(200, RoleId: role.Id) : IdentityError(created);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            return new(409, "نقشی با همین نام قبلاً ثبت شده است.");
        }
    }

    public async Task<AdminRoleWriteResult> RenameRoleAsync(string roleId, string expectedName, string name, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var role = await roles.FindByIdAsync(roleId);
        if (role is null) return new(404, "نقش پیدا نشد.");
        if (!string.Equals(role.Name, expectedName, StringComparison.Ordinal))
            return new(409, "نام نقش هم‌زمان تغییر کرده است. فهرست را تازه‌سازی کنید.");
        if (string.Equals(role.Name, name, StringComparison.Ordinal))
            return new(200, RoleId: role.Id);
        role.Name = name;
        try
        {
            var updated = await roles.UpdateAsync(role);
            return updated.Succeeded ? new(200, RoleId: role.Id) : IdentityError(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "نقش هم‌زمان تغییر کرده است. فهرست را تازه‌سازی کنید.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            return new(409, "نقشی با همین نام قبلاً ثبت شده است.");
        }
    }

    // Only permissions actually lost on deletion need a user decision. Permissions supplied
    // by another role or an explicit user override are outside the deletion decision.
    private async Task<RoleRemovalPreview?> BuildRoleRemovalPreviewAsync(string roleId, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null) return null;

        var members = await (
            from link in db.UserRoles.AsNoTracking()
            join account in db.Users.AsNoTracking() on link.UserId equals account.Id
            where link.RoleId == roleId
            select new { account.Id, account.FirstName, account.LastName, account.UserName }
        ).ToListAsync(ct);
        var userIds = members.Select(x => x.Id).ToArray();
        var memberships = await db.UserRoles.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.RoleId }).ToListAsync(ct);
        var participatingRoleIds = memberships.Select(x => x.RoleId)
            .Append(roleId).Distinct().ToArray();

        var grants = await db.AccessRolePermissions.AsNoTracking()
            .Where(x => participatingRoleIds.Contains(x.RoleId))
            .Select(x => new
            {
                x.RoleId, x.AccessPermissionId,
                x.AccessPermission.Key, x.AccessPermission.Title,
                EntityKey = x.AccessPermission.AccessEntity.Key,
                IsActive = x.AccessPermission.IsActive && x.AccessPermission.AccessEntity.IsActive
            }).ToListAsync(ct);
        var overrides = await db.AccessUserPermissions.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.AccessPermissionId, x.IsAllowed }).ToListAsync(ct);

        // Snapshot changes to memberships, grants, overrides, and active permission state.
        // The DELETE request must submit this same revision; it is rechecked in a serializable transaction.
        var snapshot = new List<string>
        {
            $"Role|{role.Id}|{role.Name}|{role.ConcurrencyStamp}"
        };
        snapshot.AddRange(members.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x =>
            $"U|{x.Id}|{x.UserName}|{x.FirstName}|{x.LastName}"));
        snapshot.AddRange(memberships.OrderBy(x => x.UserId, StringComparer.Ordinal)
            .ThenBy(x => x.RoleId, StringComparer.Ordinal)
            .Select(x => $"M|{x.UserId}|{x.RoleId}"));
        snapshot.AddRange(grants.OrderBy(x => x.RoleId, StringComparer.Ordinal)
            .ThenBy(x => x.AccessPermissionId).Select(x =>
                $"G|{x.RoleId}|{x.AccessPermissionId}|{x.Key}|{x.Title}|{x.EntityKey}|{x.IsActive}"));
        snapshot.AddRange(overrides.OrderBy(x => x.UserId, StringComparer.Ordinal)
            .ThenBy(x => x.AccessPermissionId)
            .Select(x => $"O|{x.UserId}|{x.AccessPermissionId}|{x.IsAllowed}"));
        var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", snapshot))));

        var targetGrants = grants.Where(x => x.RoleId == roleId && x.IsActive).ToList();
        var allActiveByRole = grants.Where(x => x.IsActive).ToLookup(x => x.RoleId);
        var otherSource = new HashSet<(string, Guid)>();
        foreach (var membership in memberships.Where(x => x.RoleId != roleId))
            foreach (var grant in allActiveByRole[membership.RoleId])
                otherSource.Add((membership.UserId, grant.AccessPermissionId));

        var customOverrides = overrides.Select(x => (x.UserId, x.AccessPermissionId)).ToHashSet();
        var affected = members.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Select(user =>
            {
                var name = string.Join(" ", new[] { user.FirstName, user.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
                var lost = targetGrants.Where(grant =>
                        !otherSource.Contains((user.Id, grant.AccessPermissionId)) &&
                        !customOverrides.Contains((user.Id, grant.AccessPermissionId)))
                    .OrderBy(x => x.EntityKey).ThenBy(x => x.Key)
                    .Select(x => new RoleRemovalPermission(x.AccessPermissionId,
                        x.EntityKey + "." + x.Key, x.Title)).ToArray();
                return new RoleRemovalUser(user.Id, string.IsNullOrWhiteSpace(name) ? user.UserName ?? user.Id : name,
                    user.UserName ?? "", lost);
            }).ToArray();

        return new RoleRemovalPreview(role.Id, role.Name ?? "", revision,
            members.Count, affected.Sum(x => x.Permissions.Count), affected);
    }

    public Task<RoleRemovalPreview?> GetRoleRemovalPreviewAsync(string roleId, CancellationToken ct) =>
        BuildRoleRemovalPreviewAsync(roleId, ct);

    public async Task<RoleRemovalResult> RemoveRoleAsync(string roleId, string expectedName, string revision,
        IReadOnlyList<RoleRemovalChoice> choices, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var preview = await BuildRoleRemovalPreviewAsync(roleId, ct);
                if (preview is null) return new RoleRemovalResult(404, "نقش پیدا نشد.");
                if (!string.Equals(preview.RoleName, expectedName, StringComparison.Ordinal) ||
                    !string.Equals(preview.Revision, revision, StringComparison.Ordinal))
                    return new RoleRemovalResult(409, "نقش یا مجوزهای کاربران تغییر کرده‌اند. پیش‌نمایش حذف را دوباره باز کنید.");

                var expected = preview.Users.SelectMany(u => u.Permissions
                    .Select(p => (u.Id, p.Id))).ToHashSet();
                var actual = choices.Select(x => (x.UserId, x.PermissionId)).ToHashSet();
                if (choices.Count != actual.Count || !actual.SetEquals(expected))
                    return new RoleRemovalResult(409, "برای تک‌تک مجوزهای وابسته باید تصمیم معتبر ثبت شود.");

                // Existing user overrides (allow or deny) are never overwritten.
                foreach (var choice in choices.Where(x => x.Keep))
                    db.AccessUserPermissions.Add(new AccessUserPermission
                    {
                        UserId = choice.UserId, AccessPermissionId = choice.PermissionId, IsAllowed = true
                    });

                var userRoles = await db.UserRoles.Where(x => x.RoleId == roleId).ToListAsync(ct);
                db.UserRoles.RemoveRange(userRoles);
                await db.SaveChangesAsync(ct);

                var role = await roles.FindByIdAsync(roleId);
                if (role is null) return new RoleRemovalResult(409, "نقش هم‌زمان حذف شده است.");
                var deleted = await roles.DeleteAsync(role);
                if (!deleted.Succeeded)
                    return new RoleRemovalResult(409, string.Join("؛ ", deleted.Errors.Select(e => e.Description)));
                await transaction.CommitAsync(ct);
                return new RoleRemovalResult(200);
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "نقش هم‌زمان تغییر کرده است. پیش‌نمایش حذف را دوباره دریافت کنید.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 547 or 2601 or 2627)
        {
            return new(409, "داده‌های نقش یا کاربران تغییر کرده است. پیش‌نمایش حذف را دوباره دریافت کنید.");
        }
    }

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
