using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.RequestAmounts.Entities
{
    public class RequestAmountSupplyFactor:JunctionBaseEntity
    {
        public Guid RequestAmountId { get; set; }
        public Guid SupplyFactorId { get; set; }
    }
}
