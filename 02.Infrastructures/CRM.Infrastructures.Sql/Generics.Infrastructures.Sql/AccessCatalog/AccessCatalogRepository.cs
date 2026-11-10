using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Generics.Infrastructures.Sql;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed class AccessCatalogRepository(UserContextDb db) : IAccessCatalogRepository
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

    public Task<bool> PermissionHasRoleAssignmentsAsync(Guid permissionId, CancellationToken ct) =>
        db.AccessRolePermissions.AnyAsync(x => x.AccessPermissionId == permissionId, ct);

    public async Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(string roleId, CancellationToken ct) =>
        await db.AccessRolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.AccessPermissionId).ToArrayAsync(ct);

    // Update only active permissions. Disabled permissions retain their historical associations
    // but are not editable/grantable from this screen.
    public async Task ReplaceActiveRolePermissionsAsync(string roleId, IReadOnlyCollection<Guid> activePermissionIds,
        IReadOnlyCollection<Guid> chosenIds, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var active = activePermissionIds.ToArray();
        var desired = chosenIds.ToHashSet();
        var existing = await db.AccessRolePermissions.Where(x => x.RoleId == roleId && active.Contains(x.AccessPermissionId))
            .ToListAsync(ct);
        var present = existing.Select(x => x.AccessPermissionId).ToHashSet();
        db.AccessRolePermissions.RemoveRange(existing.Where(x => !desired.Contains(x.AccessPermissionId)));
        foreach (var id in desired.Where(id => !present.Contains(id)))
            db.AccessRolePermissions.Add(new AccessRolePermission { RoleId = roleId, AccessPermissionId = id });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        { throw new AccessCatalogConflictException(); }
        await transaction.CommitAsync(ct);
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
