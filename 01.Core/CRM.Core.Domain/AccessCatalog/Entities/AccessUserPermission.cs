#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// Only deviations from the user's role-derived access are stored.
public sealed class AccessUserPermission
{
    public string UserId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    public bool IsAllowed { get; set; }
    public AccessPermission AccessPermission { get; set; } = null!;
}
