using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Supplies.Entities
{
    public class SupplyOrderHead: BaseEntity
    {


        public Guid SupplyFactorId { get; set; }
        public Guid OrderHeadId { get; set; }


    }
}
