#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// RoleId references AspNetRoles.Id in UserContextDb.
public sealed class AccessRolePermission
{
    public string RoleId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    public AccessPermission AccessPermission { get; set; } = null!;
}
