#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// Only deviations from the user's role-derived access are stored.
public sealed class AccessUserPermission
{
    public string UserId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    public bool IsAllowed { get; set; }
    // Only meaningful for explicit allow; deny always wins.
    public byte Scope { get; set; } = AccessScopes.All;
    public AccessPermission AccessPermission { get; set; } = null!;
}
