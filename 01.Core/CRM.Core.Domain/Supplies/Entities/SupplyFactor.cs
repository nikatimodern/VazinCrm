using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.Core.Domain.Supplies.Entities
{
    public class SupplyFactor:BaseEntity
    {
        public byte OfficialType { get; set; }
        public string FactorSerial { get; set; }//شماره فاکتور صادر شده توسط تامیین کننده
        public decimal Amount { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierTxt { get; set; }

        [NotMapped]
        public IList<Attachment> Attachments { get; set; }

        public DateTime FactorDate { get; set; }
        public string FactorDateTxt { get; set; }

        public IList<SupplyFactorOrderHead> SupplyFactorOrderHeads { get; set; }
        public IList<RequestAmountSupplyFactor> RequestAmountSupplyFactor { get; set; }
        public IList<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }

    }
}
