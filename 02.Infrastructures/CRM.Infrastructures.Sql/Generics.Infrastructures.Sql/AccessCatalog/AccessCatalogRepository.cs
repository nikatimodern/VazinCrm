using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CRM.Core.Domain.AppUsers.Entities;
using Generics.Infrastructures.Sql;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed class AccessCatalogRepository(UserContextDb db, UserManager<AppUser> userManager) : IAccessCatalogRepository
{
    public async Task<IReadOnlyList<AccessEntity>> ListAsync(CancellationToken ct) =>
        await db.AccessEntities.AsNoTracking().Include(x => x.Permissions).ToListAsync(ct);

    public Task<AccessEntity?> FindEntityAsync(Guid id, CancellationToken ct) =>
        db.AccessEntities.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AccessPermission?> FindPermissionAsync(Guid id, CancellationToken ct) =>
        db.AccessPermissions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> EntityKeyExistsAsync(string key, Guid? exceptId, CancellationToken ct) =>
        db.AccessEntities.AnyAsync(x => x.Key == key && (!exceptId.HasValue || x.Id != exceptId.Value), ct);

    public Task<bool> ModelExistsAsync(string contextName, string modelFullName, Guid? exceptId, CancellationToken ct) =>
        db.AccessEntities.AnyAsync(x => x.DbContextName == contextName && x.ModelFullName == modelFullName &&
                                        (!exceptId.HasValue || x.Id != exceptId.Value), ct);

    public Task<bool> PermissionKeyExistsAsync(Guid entityId, string key, Guid? exceptId, CancellationToken ct) =>
        db.AccessPermissions.AnyAsync(x => x.AccessEntityId == entityId && x.Key == key &&
                                           (!exceptId.HasValue || x.Id != exceptId.Value), ct);

    public async Task<PermissionAssignmentCounts> PermissionAssignmentsAsync(Guid permissionId, CancellationToken ct) =>
        new(
            await db.AccessRolePermissions.CountAsync(x => x.AccessPermissionId == permissionId, ct),
            await db.AccessUserPermissions.CountAsync(x => x.AccessPermissionId == permissionId, ct),
            await db.AccessUserScopeMembers.CountAsync(x => x.AccessPermissionId == permissionId, ct));

    public async Task<bool> DeletePermissionWithAssignmentsAsync(
        Guid permissionId, PermissionAssignmentCounts expected, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            // All dependency checks and deletes are isolated from concurrent grant writes.
            await using var transaction = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            var permission = await db.AccessPermissions
                .FirstOrDefaultAsync(x => x.Id == permissionId, ct);
            if (permission is null) return false;

            var roleGrants = await db.AccessRolePermissions
                .Where(x => x.AccessPermissionId == permissionId).ToListAsync(ct);
            var userOverrides = await db.AccessUserPermissions
                .Where(x => x.AccessPermissionId == permissionId).ToListAsync(ct);
            var selectedMembers = await db.AccessUserScopeMembers
                .Where(x => x.AccessPermissionId == permissionId).ToListAsync(ct);

            // A new or removed grant since the preview requires a fresh confirmation.
            if (roleGrants.Count != expected.RoleCount ||
                userOverrides.Count != expected.UserCount ||
                selectedMembers.Count != expected.SelectedMemberCount)
                return false;

            db.AccessUserScopeMembers.RemoveRange(selectedMembers);
            db.AccessUserPermissions.RemoveRange(userOverrides);
            db.AccessRolePermissions.RemoveRange(roleGrants);
            db.AccessPermissions.Remove(permission);

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                                 sql.Number is 2601 or 2627 or 547)
            {
                throw new AccessCatalogConflictException();
            }
            return true;
        });
    }

    public async Task<IReadOnlyList<AccessRoleGrant>> GetRolePermissionGrantsAsync(string roleId, CancellationToken ct) =>
        await db.AccessRolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => new AccessRoleGrant(x.RoleId, x.AccessPermissionId, x.Scope)).ToArrayAsync(ct);

    public async Task<bool> HasOwnScopeAssignmentsAsync(Guid permissionId, CancellationToken ct) =>
        await db.AccessRolePermissions.AnyAsync(x => x.AccessPermissionId == permissionId && x.Scope == AccessScopes.Own, ct) ||
        await db.AccessUserPermissions.AnyAsync(x => x.AccessPermissionId == permissionId && x.IsAllowed && (x.Scope == AccessScopes.Own || x.Scope == AccessScopes.SelectedUsers ||
                x.Scope == AccessScopes.AllExceptSelectedUsers), ct);

    // Update only active permissions. Disabled permissions retain their historical associations
    // but are not editable/grantable from this screen.
    public async Task ReplaceActiveRolePermissionsAsync(string roleId, IReadOnlyCollection<Guid> activePermissionIds,
        IReadOnlyCollection<AccessRoleGrantDraft> chosen, CancellationToken ct)
    {
        // UserContextDb enables SQL Server retry; a manual transaction must run inside its execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retried transaction must not reuse entity state from the previous failed attempt.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var active = activePermissionIds.ToArray();
            var desired = chosen.ToDictionary(x => x.AccessPermissionId, x => x.Scope);
            var existing = await db.AccessRolePermissions
                .Where(x => x.RoleId == roleId && active.Contains(x.AccessPermissionId))
                .ToListAsync(ct);
            var present = existing.Select(x => x.AccessPermissionId).ToHashSet();
            foreach (var existingGrant in existing)
            {
                if (desired.TryGetValue(existingGrant.AccessPermissionId, out var scope))
                    existingGrant.Scope = scope;
                else db.AccessRolePermissions.Remove(existingGrant);
            }
            foreach (var item in chosen.Where(x => !present.Contains(x.AccessPermissionId)))
                db.AccessRolePermissions.Add(new AccessRolePermission
                {
                    RoleId = roleId, AccessPermissionId = item.AccessPermissionId, Scope = item.Scope
                });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627 or 547)
            {
                throw new AccessCatalogConflictException();
            }
            await transaction.CommitAsync(ct);
            return true;
        });
    }

    public async Task<AccessUserState?> UserAccessAsync(string userId, CancellationToken ct)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && !x.OrganAdmin, ct))
            return null;
        var roles = await db.UserRoles.AsNoTracking()
            .Where(x => x.UserId == userId).Select(x => x.RoleId).ToArrayAsync(ct);
        var overrides = await db.AccessUserPermissions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new AccessUserOverride(x.AccessPermissionId, x.IsAllowed, x.Scope)).ToArrayAsync(ct);
        var targets = await db.AccessUserScopeMembers.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => new { x.AccessPermissionId, x.TargetUserId }).ToListAsync(ct);
        var ids = targets.Select(x => x.TargetUserId).Distinct().ToArray();
        var named = await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
            .ToDictionaryAsync(x => x.Id,
                x => new AccessScopeUser(x.Id, ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                    x.UserName ?? ""), ct);
        var groups = targets.GroupBy(x => x.AccessPermissionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetUserId).OrderBy(id => id).ToArray());
        return new AccessUserState(userId, roles, overrides.Select(x =>
        {
            var members = groups.GetValueOrDefault(x.AccessPermissionId) ?? [];
            return x with { SelectedUserIds = members, SelectedUsers = members.Select(id =>
                named.GetValueOrDefault(id) ?? new AccessScopeUser(id, id, "")).ToArray() };
        }).ToArray());
    }

    public async Task<IReadOnlyList<AccessScopeUser>> SearchScopeUsersAsync(string search, CancellationToken ct)
    {
        search = search.Trim();
        if (search.Length is < 2 or > 100) return [];
        var like = "%" + search + "%";
        return await db.Users.AsNoTracking().Where(x => x.IsEmployee && !x.OrganAdmin &&
                (EF.Functions.Like(x.FirstName, like) || EF.Functions.Like(x.LastName, like) ||
                 EF.Functions.Like(x.UserName!, like) ||
                 EF.Functions.Like((x.FirstName ?? "") + " " + (x.LastName ?? ""), like)))
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Id)
            .Select(x => new AccessScopeUser(x.Id, ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                x.UserName ?? "")).Take(25).ToArrayAsync(ct);
    }

    public async Task<bool> ValidateScopeUsersAsync(string ownerUserId, IReadOnlyCollection<string> users, CancellationToken ct)
    {
        var ids = users.ToArray();
        if (ids.Length is < 1 or > 100 ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Length ||
            ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 450 ||
                          id == ownerUserId || !Guid.TryParse(id, out _)))
            return false;
        return await db.Users.AsNoTracking().CountAsync(x => ids.Contains(x.Id) &&
            x.IsEmployee && !x.OrganAdmin, ct) == ids.Length;
    }

    public async Task<IReadOnlyList<AccessRoleGrant>> RoleGrantsAsync(CancellationToken ct) =>
        await db.AccessRolePermissions.AsNoTracking()
            .Where(x => x.AccessPermission.IsActive && x.AccessPermission.AccessEntity.IsActive)
            .Select(x => new AccessRoleGrant(x.RoleId, x.AccessPermissionId, x.Scope))
            .ToArrayAsync(ct);

    public async Task<AccessUserWriteResult> ReplaceUserAccessAsync(
        string userId, IReadOnlyCollection<string> roleIds, IReadOnlyCollection<AccessUserOverride> overrides,
        IReadOnlyCollection<Guid> activePermissionIds, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.OrganAdmin) return new AccessUserWriteResult(false, NotFound: true);

            var selected = roleIds.ToHashSet(StringComparer.Ordinal);
            var roleMap = await db.Roles.AsNoTracking().Where(x => selected.Contains(x.Id))
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            if (roleMap.Count != selected.Count || roleMap.Any(r => string.IsNullOrWhiteSpace(r.Name)))
                return new AccessUserWriteResult(false, Error: "یک یا چند نقش دیگر وجود ندارد.");

            var desiredNames = roleMap.Select(x => x.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var currentNames = (await userManager.GetRolesAsync(user)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var remove = currentNames.Except(desiredNames, StringComparer.OrdinalIgnoreCase).ToArray();
            var add = desiredNames.Except(currentNames, StringComparer.OrdinalIgnoreCase).ToArray();

            if (remove.Length != 0)
            {
                var result = await userManager.RemoveFromRolesAsync(user, remove);
                if (!result.Succeeded) return new AccessUserWriteResult(false,
                    Error: string.Join("؛ ", result.Errors.Select(e => e.Description)));
            }
            if (add.Length != 0)
            {
                var result = await userManager.AddToRolesAsync(user, add);
                if (!result.Succeeded) return new AccessUserWriteResult(false,
                    Error: string.Join("؛ ", result.Errors.Select(e => e.Description)));
            }

            // Only active permission overrides are editable; retain inactive historical overrides.
            var active = activePermissionIds.ToHashSet();
            var requested = overrides.ToDictionary(x => x.AccessPermissionId);
            var selectedMembers = overrides.Where(x => x.IsAllowed && x.Scope is AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers)
                .SelectMany(x => (x.SelectedUserIds ?? []).Select(id =>
                    (PermissionId: x.AccessPermissionId, TargetUserId: id))).ToHashSet();
            var links = await db.AccessUserScopeMembers.Where(x => x.UserId == userId &&
                active.Contains(x.AccessPermissionId)).ToListAsync(ct);
            var existing = links.Select(x => (PermissionId: x.AccessPermissionId, TargetUserId: x.TargetUserId)).ToHashSet();
            db.AccessUserScopeMembers.RemoveRange(links.Where(x =>
                !selectedMembers.Contains((x.AccessPermissionId, x.TargetUserId))));
            foreach (var member in selectedMembers.Except(existing))
                db.AccessUserScopeMembers.Add(new AccessUserScopeMember
                {
                    UserId = userId, AccessPermissionId = member.PermissionId, TargetUserId = member.TargetUserId
                });
            var current = await db.AccessUserPermissions
                .Where(x => x.UserId == userId && active.Contains(x.AccessPermissionId)).ToListAsync(ct);
            foreach (var item in current)
            {
                if (requested.Remove(item.AccessPermissionId, out var desired))
                {
                    item.IsAllowed = desired.IsAllowed;
                    item.Scope = desired.Scope;
                }
                else
                {
                    db.AccessUserPermissions.Remove(item);
                }
            }
            foreach (var pair in requested)
                db.AccessUserPermissions.Add(new AccessUserPermission
                {
                    UserId = userId, AccessPermissionId = pair.Key, IsAllowed = pair.Value.IsAllowed,
                    Scope = pair.Value.Scope
                });

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                                sql.Number is 2601 or 2627 or 547)
            {
                throw new AccessCatalogConflictException();
            }
            return new AccessUserWriteResult(true);
        });
    }

    public void Add(AccessEntity entity) => db.AccessEntities.Add(entity);
    public void Add(AccessPermission permission) => db.AccessPermissions.Add(permission);
    public void Remove(AccessEntity entity) => db.AccessEntities.Remove(entity);
    public void Remove(AccessPermission permission) => db.AccessPermissions.Remove(permission);

    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            throw new AccessCatalogConflictException();
        }
    }
}
