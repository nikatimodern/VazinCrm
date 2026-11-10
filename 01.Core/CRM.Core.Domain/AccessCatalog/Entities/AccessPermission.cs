#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

public sealed class AccessPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccessEntityId { get; set; }
    public AccessEntity AccessEntity { get; set; } = null!;
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
