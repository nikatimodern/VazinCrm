using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Supplies.Entities
{
    public class SupplyFactorOrderHead:JunctionBaseEntity {
        public Guid SupplyFactorId { get; set; } = Guid.Empty;
    
        public Guid OrderHeadId { get; set; }
    }
}
