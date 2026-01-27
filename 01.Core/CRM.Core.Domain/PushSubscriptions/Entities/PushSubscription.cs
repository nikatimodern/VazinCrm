using System;

namespace CRM.Core.Domain.PushSubscriptions.Entities;

public class PushSubscription {
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Endpoint { get; set; }
    public string P256dh { get; set; }
    public string Auth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}