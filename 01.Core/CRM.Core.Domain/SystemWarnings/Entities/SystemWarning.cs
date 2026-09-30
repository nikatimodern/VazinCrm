using System;
using CRM.Core.Domain.SystemWarnings.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.SystemWarnings.Entities
{
    public class SystemWarning:BaseEntity
    {
        public string Message { get; set; }
        public EntityTypeEnum EntityType { get; set; }
        public WarningTypeEnum WarningType { get; set; }
        public string WarningTypeTxt { get; set; }
        public string EntityTypeTxt { get; set; }
        public Guid EntityId { get; set; }
        public bool Resolve { get; set; } = false;
    }
}
