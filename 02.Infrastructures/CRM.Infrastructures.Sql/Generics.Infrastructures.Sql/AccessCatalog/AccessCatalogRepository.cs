using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed class AccessCatalogRepository(CRMContextDb db) : IAccessCatalogRepository
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
