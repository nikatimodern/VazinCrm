using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Emails.Entities;

public class Email : BaseEntity {
    public string EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string FromName { get; set; }
    public Guid FromId { get; set; }
    public string ToName { get; set; }
    public Guid ToId { get; set; }
    public bool Seen { get; set; }
}