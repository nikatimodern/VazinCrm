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
            return AccessScopes.IsGrant(custom.Scope) &&
                   (custom.Scope != AccessScopes.Own || permission.SupportsScope)
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

    public async Task<bool> CanAccessCustomerAsync(
        string userId, string permissionKey, Guid customerId, CancellationToken ct)
    {
        if (customerId == Guid.Empty) return false;
        var scope = await GetEffectiveScopeAsync(userId, permissionKey, ct);
        if (scope == AccessScopes.All) return true;
        // UserCustomers.UserId is a Guid, while Identity's Id is string.
        return scope == AccessScopes.Own && Guid.TryParse(userId, out var id) &&
               await crm.UserCustomers.AsNoTracking()
                   .AnyAsync(x => x.UserId == id && x.CustomerId == customerId, ct);
    }
}
