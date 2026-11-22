#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// RoleId references AspNetRoles.Id in UserContextDb.
public sealed class AccessRolePermission
{
    public string RoleId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    // Existing grants retain their former unrestricted meaning.
    public byte Scope { get; set; } = AccessScopes.All;
    public AccessPermission AccessPermission { get; set; } = null!;
}
