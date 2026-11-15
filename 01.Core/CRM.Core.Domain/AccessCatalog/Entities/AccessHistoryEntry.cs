#nullable enable
using System;

namespace CRM.Core.Domain.AccessCatalog.Entities;

// Only access policy is versioned. No passwords or personal user profile fields.
public sealed class AccessHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Action { get; set; } = "";
    public string Title { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string? AfterJson { get; set; }
    public int ChangeCount { get; set; }
}
