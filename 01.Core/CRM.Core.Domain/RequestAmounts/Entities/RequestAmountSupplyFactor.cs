using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.Core.Domain.RequestAmounts.Entities {
    public class RequestAmountSupplyFactor : JunctionBaseEntity {
        public Guid RequestAmountId { get; set; }
        public Guid SupplyFactorId { get; set; }

        public RequestAmount RequestAmount { get; set; }
        public SupplyFactor SupplyFactor { get; set; }
    }
}