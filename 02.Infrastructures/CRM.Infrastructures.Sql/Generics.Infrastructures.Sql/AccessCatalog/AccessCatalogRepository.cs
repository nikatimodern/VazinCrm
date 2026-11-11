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

    public async Task<bool> PermissionHasRoleAssignmentsAsync(Guid permissionId, CancellationToken ct) =>
        await db.AccessRolePermissions.AnyAsync(x => x.AccessPermissionId == permissionId, ct) ||
        await db.AccessUserPermissions.AnyAsync(x => x.AccessPermissionId == permissionId, ct);

    public async Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(string roleId, CancellationToken ct) =>
        await db.AccessRolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.AccessPermissionId).ToArrayAsync(ct);

    // Update only active permissions. Disabled permissions retain their historical associations
    // but are not editable/grantable from this screen.
    public async Task ReplaceActiveRolePermissionsAsync(string roleId, IReadOnlyCollection<Guid> activePermissionIds,
        IReadOnlyCollection<Guid> chosenIds, CancellationToken ct)
    {
        // UserContextDb enables SQL Server retry; a manual transaction must run inside its execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A retried transaction must not reuse entity state from the previous failed attempt.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var active = activePermissionIds.ToArray();
            var desired = chosenIds.ToHashSet();
            var existing = await db.AccessRolePermissions
                .Where(x => x.RoleId == roleId && active.Contains(x.AccessPermissionId))
                .ToListAsync(ct);
            var present = existing.Select(x => x.AccessPermissionId).ToHashSet();
            db.AccessRolePermissions.RemoveRange(existing.Where(x => !desired.Contains(x.AccessPermissionId)));
            foreach (var id in desired.Where(id => !present.Contains(id)))
                db.AccessRolePermissions.Add(new AccessRolePermission { RoleId = roleId, AccessPermissionId = id });
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
            .Select(x => new AccessUserOverride(x.AccessPermissionId, x.IsAllowed)).ToArrayAsync(ct);
        return new AccessUserState(userId, roles, overrides);
    }

    public async Task<IReadOnlyList<AccessRoleGrant>> RoleGrantsAsync(CancellationToken ct) =>
        await db.AccessRolePermissions.AsNoTracking()
            .Where(x => x.AccessPermission.IsActive && x.AccessPermission.AccessEntity.IsActive)
            .Select(x => new AccessRoleGrant(x.RoleId, x.AccessPermissionId))
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
            var current = await db.AccessUserPermissions
                .Where(x => x.UserId == userId && active.Contains(x.AccessPermissionId)).ToListAsync(ct);
            foreach (var item in current)
            {
                if (requested.Remove(item.AccessPermissionId, out var desired))
                    item.IsAllowed = desired.IsAllowed;
                else
                    db.AccessUserPermissions.Remove(item);
            }
            foreach (var pair in requested)
                db.AccessUserPermissions.Add(new AccessUserPermission
                {
                    UserId = userId, AccessPermissionId = pair.Key, IsAllowed = pair.Value.IsAllowed
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
