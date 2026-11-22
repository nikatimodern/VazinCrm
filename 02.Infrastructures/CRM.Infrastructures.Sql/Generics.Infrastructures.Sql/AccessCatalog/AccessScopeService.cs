#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.AccessCatalog;

/// <summary>
/// Evaluates role union (widest scope wins), then replaces it with explicit
/// user override if present; explicit deny always wins.
/// Fail-closed for unknown/inactive permissions, users, and missing ownership.
/// This does NOT automatically enforce endpoints. Call it before each query/mutation.
/// </summary>
public sealed class AccessScopeService(UserContextDb users, CRMContextDb crm) : IAccessScopeService
{
    public async Task<byte> GetEffectiveScopeAsync(string userId, string permissionKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(permissionKey))
            return AccessScopes.None;
        var dot = permissionKey.IndexOf('.');
        if (dot <= 0 || dot == permissionKey.Length - 1 ||
            permissionKey.IndexOf('.', dot + 1) >= 0)
            return AccessScopes.None;
        var entityKey = permissionKey[..dot];
        var key = permissionKey[(dot + 1)..];
        var permission = await users.AccessPermissions.AsNoTracking()
            .Where(p => p.Key == key && p.IsActive && p.AccessEntity.IsActive &&
                        p.AccessEntity.Key == entityKey)
            .Select(p => new { p.Id, p.SupportsScope })
            .FirstOrDefaultAsync(ct);
        if (permission is null || !await users.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId, ct))
            return AccessScopes.None;

        var custom = await users.AccessUserPermissions.AsNoTracking()
            .Where(x => x.UserId == userId && x.AccessPermissionId == permission.Id)
            .Select(x => new { x.IsAllowed, x.Scope }).FirstOrDefaultAsync(ct);
        if (custom is not null)
        {
            if (!custom.IsAllowed) return AccessScopes.None;
            return AccessScopes.IsOverrideGrant(custom.Scope) &&
                   (custom.Scope == AccessScopes.All || permission.SupportsScope) &&
                   (custom.Scope != AccessScopes.SelectedUsers || entityKey == "Customer")
                ? custom.Scope : AccessScopes.None;
        }

        var scopes = await (
            from member in users.UserRoles.AsNoTracking()
            join grant in users.AccessRolePermissions.AsNoTracking()
                on member.RoleId equals grant.RoleId
            where member.UserId == userId && grant.AccessPermissionId == permission.Id
            select grant.Scope).ToArrayAsync(ct);
        if (scopes.Length == 0) return AccessScopes.None;
        // Validate rather than trusting potentially corrupt persisted scope values.
        var valid = scopes.Where(s => AccessScopes.IsGrant(s) &&
                                     (s != AccessScopes.Own || permission.SupportsScope));
        return valid.DefaultIfEmpty(AccessScopes.None).Max();
    }

    public async Task<Guid[]?> GetAllowedCustomerOwnerIdsAsync(
        string userId, string permissionKey, CancellationToken ct)
    {
        if (!permissionKey.StartsWith("Customer.", StringComparison.Ordinal)) return [];
        var scope = await GetEffectiveScopeAsync(userId, permissionKey, ct);
        if (scope == AccessScopes.All) return null;
        if (scope == AccessScopes.None || !Guid.TryParse(userId, out var ownId)) return [];
        if (scope == AccessScopes.Own) return [ownId];

        // Only a *user-specific* SelectedUsers override can reach this branch.
        var key = permissionKey["Customer.".Length..];
        var selected = await (from link in users.AccessUserScopeMembers.AsNoTracking()
            join perm in users.AccessPermissions.AsNoTracking() on link.AccessPermissionId equals perm.Id
            join target in users.Users.AsNoTracking() on link.TargetUserId equals target.Id
            where link.UserId == userId && perm.Key == key && perm.AccessEntity.Key == "Customer" &&
                  target.IsEmployee && !target.OrganAdmin
            select link.TargetUserId).ToArrayAsync(ct);
        return selected.Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty).Append(ownId).Distinct().ToArray();
    }

    public async Task<bool> CanAccessCustomerAsync(
        string userId, string permissionKey, Guid customerId, CancellationToken ct)
    {
        if (customerId == Guid.Empty || !permissionKey.StartsWith("Customer.", StringComparison.Ordinal)) return false;
        var allowed = await GetAllowedCustomerOwnerIdsAsync(userId, permissionKey, ct);
        if (allowed is null) return true;
        if (allowed.Length == 0) return false;
        return await crm.UserCustomers.AsNoTracking().AnyAsync(link =>
            link.CustomerId == customerId && allowed.Contains(link.UserId), ct);
    }
}
