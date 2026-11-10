#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// RoleId comes from the Identity database, so it intentionally has no cross-database FK.
public sealed class AccessRolePermission
{
    public string RoleId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    public AccessPermission AccessPermission { get; set; } = null!;
}
