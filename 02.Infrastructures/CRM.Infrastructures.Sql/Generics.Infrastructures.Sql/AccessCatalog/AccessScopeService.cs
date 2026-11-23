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
                   (custom.Scope is not (AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers) ||
                    entityKey is "Customer" or "Deal" or "PreFactor" or "Factor" or "OrderHead" or "Supply" or "SupplyFactor" or "Accountancy" or "RequestAmount" or "Payment" or "BankAccount" or "Checkbook" or "Check")
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

    // Retained for callers expecting inclusive-only filtering: the exclusion scope
    // must never be mistaken for All by legacy callers.
    public async Task<Guid[]?> GetAllowedCustomerOwnerIdsAsync(
        string userId, string permissionKey, CancellationToken ct)
    {
        var filter = await GetCustomerOwnerAccessAsync(userId, permissionKey, ct);
        return filter.ExcludedOwnerIds.Length > 0 ? [] : filter.AllowedOwnerIds;
    }

    public async Task<CustomerOwnerAccess> GetCustomerOwnerAccessAsync(
        string userId, string permissionKey, CancellationToken ct)
    {
        if (!permissionKey.StartsWith("Customer.", StringComparison.Ordinal))
            return new([], []);
        var scope = await GetEffectiveScopeAsync(userId, permissionKey, ct);
        if (scope == AccessScopes.All) return new(null, []);
        if (scope == AccessScopes.None || !Guid.TryParse(userId, out var ownId))
            return new([], []);
        if (scope == AccessScopes.Own) return new([ownId], []);

        var key = permissionKey["Customer.".Length..];
        // For exclusions, do not join with the target's current employee status:
        // historical assignments must remain blocked even if an employee leaves.
        var selectedIds = await (from link in users.AccessUserScopeMembers.AsNoTracking()
            join perm in users.AccessPermissions.AsNoTracking() on link.AccessPermissionId equals perm.Id
            where link.UserId == userId && perm.Key == key && perm.AccessEntity.Key == "Customer"
            select link.TargetUserId).ToArrayAsync(ct);
        if (selectedIds.Length == 0 || selectedIds.Any(id => !Guid.TryParse(id, out _)))
            return new([], []); // corrupted or missing selection must fail closed
        var ids = selectedIds.Select(Guid.Parse).Distinct().ToArray();

        if (scope == AccessScopes.AllExceptSelectedUsers)
            return new(null, ids);
        if (scope != AccessScopes.SelectedUsers) return new([], []);

        var validTargetIds = await users.Users.AsNoTracking()
            .Where(u => selectedIds.Contains(u.Id) && u.IsEmployee && !u.OrganAdmin)
            .Select(u => u.Id).ToArrayAsync(ct);
        var allowed = validTargetIds.Select(Guid.Parse).Append(ownId).Distinct().ToArray();
        return new(allowed, []);
    }

    public Task<DealOwnerAccess> GetDealOwnerAccessAsync(
        string userId, string permissionKey, CancellationToken ct) =>
        permissionKey.StartsWith("Deal.", StringComparison.Ordinal)
            ? GetSingleOwnerAccessAsync(userId, permissionKey, ct)
            : Task.FromResult(new DealOwnerAccess([], []));

    public Task<DealOwnerAccess> GetDocumentOwnerAccessAsync(
        string userId, string permissionKey, CancellationToken ct) =>
        permissionKey.StartsWith("PreFactor.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Factor.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("OrderHead.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Supply.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("SupplyFactor.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Accountancy.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("RequestAmount.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Payment.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("BankAccount.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Checkbook.", StringComparison.Ordinal) ||
        permissionKey.StartsWith("Check.", StringComparison.Ordinal)
            ? GetSingleOwnerAccessAsync(userId, permissionKey, ct)
            : Task.FromResult(new DealOwnerAccess([], []));

    private async Task<DealOwnerAccess> GetSingleOwnerAccessAsync(
        string userId, string permissionKey, CancellationToken ct)
    {
        var dot = permissionKey.IndexOf('.');
        var entity = permissionKey[..dot];
        var scope = await GetEffectiveScopeAsync(userId, permissionKey, ct);
        if (scope == AccessScopes.All) return new(null, []);
        if (scope == AccessScopes.None || !Guid.TryParse(userId, out _))
            return new([], []);
        if (scope == AccessScopes.Own) return new([userId], []);
        if (scope is not (AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers))
            return new([], []);

        var key = permissionKey[(dot + 1)..];
        var selected = await (from link in users.AccessUserScopeMembers.AsNoTracking()
            join perm in users.AccessPermissions.AsNoTracking()
                on link.AccessPermissionId equals perm.Id
            where link.UserId == userId && perm.Key == key && perm.AccessEntity.Key == entity
            select link.TargetUserId).ToArrayAsync(ct);
        // Empty, corrupt or self-referential exclusions must never widen access.
        if (selected.Length == 0 ||
            selected.Any(id => !Guid.TryParse(id, out _) ||
                               string.Equals(id, userId, StringComparison.OrdinalIgnoreCase)))
            return new([], []);
        var validEmployeeIds = await users.Users.AsNoTracking()
            .Where(u => selected.Contains(u.Id) && u.IsEmployee && !u.OrganAdmin)
            .Select(u => u.Id).ToArrayAsync(ct);
        if (validEmployeeIds.Length != selected.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return new([], []);
        if (scope == AccessScopes.AllExceptSelectedUsers)
            return new(null, selected.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        return new(validEmployeeIds.Append(userId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), []);
    }

    public async Task<bool> CanAccessDealAsync(
        string userId, string permissionKey, Guid dealId, CancellationToken ct)
    {
        if (dealId == Guid.Empty || !permissionKey.StartsWith("Deal.", StringComparison.Ordinal))
            return false;
        var filter = await GetDealOwnerAccessAsync(userId, permissionKey, ct);
        if (filter.AllowedOwnerIds is { Length: 0 }) return false;
        var query = crm.Deals.AsNoTracking().Where(x => x.Id == dealId);
        if (filter.AllowedOwnerIds is { } allowed)
            query = query.Where(x => allowed.Contains(x.UserCreatorID));
        if (filter.ExcludedOwnerIds.Length > 0)
        {
            var excluded = filter.ExcludedOwnerIds;
            query = query.Where(x => !excluded.Contains(x.UserCreatorID));
        }
        return await query.AnyAsync(ct);
    }

    public async Task<bool> CanAccessCustomerAsync(
        string userId, string permissionKey, Guid customerId, CancellationToken ct)
    {
        if (customerId == Guid.Empty || !permissionKey.StartsWith("Customer.", StringComparison.Ordinal))
            return false;
        var filter = await GetCustomerOwnerAccessAsync(userId, permissionKey, ct);
        var allowed = filter.AllowedOwnerIds;
        var excluded = filter.ExcludedOwnerIds;
        if (allowed is { Length: 0 }) return false;
        if (allowed is null && excluded.Length == 0) return true;

        // Separate query shapes so EF never has to translate a null-array Contains.
        // Any excluded owner's assignment blocks this person, even if shared.
        if (allowed is null)
            return await crm.Customers.AsNoTracking().AnyAsync(c =>
                c.Id == customerId &&
                !crm.UserCustomers.Any(link => link.CustomerId == c.Id &&
                                               excluded.Contains(link.UserId)), ct);
        if (excluded.Length == 0)
            return await crm.UserCustomers.AsNoTracking().AnyAsync(link =>
                link.CustomerId == customerId && allowed.Contains(link.UserId), ct);
        return await crm.UserCustomers.AsNoTracking().AnyAsync(link =>
            link.CustomerId == customerId && allowed.Contains(link.UserId) &&
            !crm.UserCustomers.Any(blocked => blocked.CustomerId == customerId &&
                                              excluded.Contains(blocked.UserId)), ct);
    }
}
