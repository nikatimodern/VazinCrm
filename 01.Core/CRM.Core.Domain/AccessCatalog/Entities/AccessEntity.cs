using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.AccessCatalog.Entities;

public sealed class AccessEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string DbContextName { get; set; } = "";
    public string ModelFullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AccessPermission> Permissions { get; set; } = new List<AccessPermission>();
}
