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


public sealed record AccessScopeUser(string Id, string Name, string UserName);
public sealed record AccessUserOverride(Guid AccessPermissionId, bool IsAllowed, byte Scope = AccessScopes.None,
    string[]? SelectedUserIds = null, AccessScopeUser[]? SelectedUsers = null);
public sealed record AccessRoleGrant(string RoleId, Guid AccessPermissionId, byte Scope = AccessScopes.All);
public sealed record AccessRoleGrantDraft(Guid AccessPermissionId, byte Scope);
public sealed record AccessUserState(string UserId, IReadOnlyList<string> RoleIds, IReadOnlyList<AccessUserOverride> Overrides);
public sealed record AccessUserWriteResult(bool Success, bool NotFound = false, string? Error = null);
public sealed record PermissionAssignmentCounts(int RoleCount, int UserCount, int SelectedMemberCount);

public interface IAccessCatalogRepository
{
    Task<IReadOnlyList<AccessEntity>> ListAsync(CancellationToken ct);
    Task<AccessEntity?> FindEntityAsync(Guid id, CancellationToken ct);
    Task<AccessPermission?> FindPermissionAsync(Guid id, CancellationToken ct);
    Task<bool> EntityKeyExistsAsync(string key, Guid? exceptId, CancellationToken ct);
    Task<bool> ModelExistsAsync(string contextName, string modelFullName, Guid? exceptId, CancellationToken ct);
    Task<bool> PermissionKeyExistsAsync(Guid entityId, string key, Guid? exceptId, CancellationToken ct);
    Task<PermissionAssignmentCounts> PermissionAssignmentsAsync(Guid permissionId, CancellationToken ct);
    Task<bool> DeletePermissionWithAssignmentsAsync(Guid permissionId, PermissionAssignmentCounts expected, CancellationToken ct);
    Task<AccessUserState?> UserAccessAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<AccessRoleGrant>> RoleGrantsAsync(CancellationToken ct);
    Task<IReadOnlyList<AccessScopeUser>> SearchScopeUsersAsync(string search, CancellationToken ct);
    Task<bool> ValidateScopeUsersAsync(string ownerUserId, IReadOnlyCollection<string> ids, CancellationToken ct);
    Task<AccessUserWriteResult> ReplaceUserAccessAsync(
        string userId, IReadOnlyCollection<string> roleIds, IReadOnlyCollection<AccessUserOverride> overrides,
        IReadOnlyCollection<Guid> activePermissionIds, CancellationToken ct);
    Task<IReadOnlyList<AccessRoleGrant>> GetRolePermissionGrantsAsync(string roleId, CancellationToken ct);
    Task ReplaceActiveRolePermissionsAsync(string roleId, IReadOnlyCollection<Guid> activePermissionIds,
        IReadOnlyCollection<AccessRoleGrantDraft> chosen, CancellationToken ct);
    Task<bool> HasOwnScopeAssignmentsAsync(Guid permissionId, CancellationToken ct);
    void Add(AccessEntity entity);
    void Add(AccessPermission permission);
    void Remove(AccessEntity entity);
    void Remove(AccessPermission permission);
    Task SaveAsync(CancellationToken ct);
}
