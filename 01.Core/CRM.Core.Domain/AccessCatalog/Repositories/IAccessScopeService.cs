#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
namespace CRM.Core.Domain.AccessCatalog.Repositories;

/// <summary>
/// Effective scope of one exact registered permission; selected-user scopes are per-user overrides.
/// Own requires a separately defined ownership check for each entity.
/// </summary>
public sealed record CustomerOwnerAccess(Guid[]? AllowedOwnerIds, Guid[] ExcludedOwnerIds);

/// <summary>
/// Deal ownership is Deal.UserCreatorID (Identity user ID), not OrderHead ownership.
/// Null allowed list means All; empty means None. Exclusions always override inclusions.
/// </summary>
public sealed record DealOwnerAccess(string[]? AllowedOwnerIds, string[] ExcludedOwnerIds)
{
    public bool Allows(string? ownerId) =>
        AllowedOwnerIds is not { Length: 0 } &&
        (AllowedOwnerIds is null ||
         AllowedOwnerIds.Contains(ownerId ?? "", StringComparer.OrdinalIgnoreCase)) &&
        !ExcludedOwnerIds.Contains(ownerId ?? "", StringComparer.OrdinalIgnoreCase);
}

public interface IAccessScopeService
{
    Task<byte> GetEffectiveScopeAsync(string userId, string permissionKey, CancellationToken ct);
    Task<bool> CanAccessCustomerAsync(string userId, string permissionKey, Guid customerId, CancellationToken ct);
    // null means All, an empty array means None; otherwise database queries must
    // restrict rows to objects assigned to one of these Identity user IDs.
    Task<Guid[]?> GetAllowedCustomerOwnerIdsAsync(string userId, string permissionKey, CancellationToken ct);
    // A null allowed list means all candidates; exclusions always win over inclusions.
    // An empty allowed array means no access (fail-closed).
    Task<CustomerOwnerAccess> GetCustomerOwnerAccessAsync(string userId, string permissionKey, CancellationToken ct);
    Task<DealOwnerAccess> GetDealOwnerAccessAsync(string userId, string permissionKey, CancellationToken ct);
    Task<DealOwnerAccess> GetDocumentOwnerAccessAsync(string userId, string permissionKey, CancellationToken ct);
    Task<bool> CanAccessDealAsync(string userId, string permissionKey, Guid dealId, CancellationToken ct);
}
