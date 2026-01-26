using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.SystemWarnings.Entities
{
    public class SystemWarning:BaseEntity
    {
        public string Message { get; set; }
        public EntityTypeEnum EntityType { get; set; }
        public string EntityTypeTxt { get; set; }
        public Guid EntityId { get; set; }
        public bool Resolve { get; set; } = false;
    }
}
