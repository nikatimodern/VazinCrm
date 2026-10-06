using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Payments.Entities
{
    public class PaymentSupplyFactor:JunctionBaseEntity
    {
        public Guid SupplyFactorId { get; set; }
        public Guid PaymentId { get; set; }

    }
}
