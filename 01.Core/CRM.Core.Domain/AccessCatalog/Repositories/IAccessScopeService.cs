#nullable enable
using CRM.Core.Domain.AccessCatalog.Entities;
namespace CRM.Core.Domain.AccessCatalog.Repositories;

/// <summary>
/// Effective scope of one exact registered permission: None/Own/All.
/// Own requires a separately defined ownership check for each entity.
/// </summary>
public interface IAccessScopeService
{
    Task<byte> GetEffectiveScopeAsync(string userId, string permissionKey, CancellationToken ct);
    Task<bool> CanAccessCustomerAsync(string userId, string permissionKey, Guid customerId, CancellationToken ct);
}
