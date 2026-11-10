using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;

namespace CRM.Core.Domain.AccessCatalog.Repositories;

public sealed class AccessCatalogConflictException : Exception
{
    public AccessCatalogConflictException() : base("Access catalog key already exists.") { }
}

public interface IAccessCatalogRepository
{
    Task<IReadOnlyList<AccessEntity>> ListAsync(CancellationToken ct);
    Task<AccessEntity?> FindEntityAsync(Guid id, CancellationToken ct);
    Task<AccessPermission?> FindPermissionAsync(Guid id, CancellationToken ct);
    Task<bool> EntityKeyExistsAsync(string key, Guid? exceptId, CancellationToken ct);
    Task<bool> ModelExistsAsync(string contextName, string modelFullName, Guid? exceptId, CancellationToken ct);
    Task<bool> PermissionKeyExistsAsync(Guid entityId, string key, Guid? exceptId, CancellationToken ct);
    void Add(AccessEntity entity);
    void Add(AccessPermission permission);
    void Remove(AccessEntity entity);
    void Remove(AccessPermission permission);
    Task SaveAsync(CancellationToken ct);
}
