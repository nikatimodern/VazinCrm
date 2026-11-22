#nullable enable
using System;
namespace CRM.Core.Domain.AccessCatalog.Entities;

/// <summary>Explicit delegated owners for one user and permission; self is implicit.</summary>
public sealed class AccessUserScopeMember
{
    public string UserId { get; set; } = "";
    public Guid AccessPermissionId { get; set; }
    public string TargetUserId { get; set; } = "";
    public AccessUserPermission AccessUserPermission { get; set; } = null!;
}
