using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Payments.Entities
{
    public class Payment:BaseEntity
    {
        public decimal Amount { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public Guid? SupplyFactorId { get; set; }
        public IList<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }

    }
}
