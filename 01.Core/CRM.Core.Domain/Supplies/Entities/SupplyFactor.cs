using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Deals.Entities;

namespace CRM.Core.Domain.Supplies.Entities {
    public class SupplyFactor : BaseEntity {
        public byte OfficialType { get; set; }
        public string SerialNumber { get; set; } //شماره فاکتور صادر شده توسط تامین کننده
        public decimal PaymentAmount { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierTxt { get; set; }

        [NotMapped] public IList<Attachment> Attachments { get; set; }

        public DateTime DateMiladi { get; set; }
        public string Date { get; set; }

        public Guid? DealId { get; set; }
        public Deal Deal { get; set; }


        public decimal SumTotalAmount { get; set; } // مبلغ کل قبل از تخفیف و مالیات
        public decimal Discount { get; set; }
        public decimal TotalTax { get; set; }
        public string Description { get; set; }


        public IList<SupplyFactorItem> Items { get; set; }


        public IList<SupplyFactorOrderHead> SupplyFactorOrderHeads { get; set; }
        public IList<RequestAmountSupplyFactor> RequestAmountSupplyFactor { get; set; }
        public IList<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }
    }
}