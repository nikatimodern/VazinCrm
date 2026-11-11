#nullable enable
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


public sealed record AccessUserOverride(Guid AccessPermissionId, bool IsAllowed);
public sealed record AccessUserState(string UserId, IReadOnlyList<string> RoleIds, IReadOnlyList<AccessUserOverride> Overrides);
public sealed record AccessRoleGrant(string RoleId, Guid AccessPermissionId);
public sealed record AccessUserWriteResult(bool Success, bool NotFound = false, string? Error = null);

public interface IAccessCatalogRepository
{
    Task<IReadOnlyList<AccessEntity>> ListAsync(CancellationToken ct);
    Task<AccessEntity?> FindEntityAsync(Guid id, CancellationToken ct);
    Task<AccessPermission?> FindPermissionAsync(Guid id, CancellationToken ct);
    Task<bool> EntityKeyExistsAsync(string key, Guid? exceptId, CancellationToken ct);
    Task<bool> ModelExistsAsync(string contextName, string modelFullName, Guid? exceptId, CancellationToken ct);
    Task<bool> PermissionKeyExistsAsync(Guid entityId, string key, Guid? exceptId, CancellationToken ct);
    Task<bool> PermissionHasRoleAssignmentsAsync(Guid permissionId, CancellationToken ct);
    Task<AccessUserState?> UserAccessAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<AccessRoleGrant>> RoleGrantsAsync(CancellationToken ct);
    Task<AccessUserWriteResult> ReplaceUserAccessAsync(
        string userId, IReadOnlyCollection<string> roleIds, IReadOnlyCollection<AccessUserOverride> overrides,
        IReadOnlyCollection<Guid> activePermissionIds, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(string roleId, CancellationToken ct);
    Task ReplaceActiveRolePermissionsAsync(string roleId, IReadOnlyCollection<Guid> activePermissionIds, IReadOnlyCollection<Guid> chosenIds, CancellationToken ct);
    void Add(AccessEntity entity);
    void Add(AccessPermission permission);
    void Remove(AccessEntity entity);
    void Remove(AccessPermission permission);
    Task SaveAsync(CancellationToken ct);
}
