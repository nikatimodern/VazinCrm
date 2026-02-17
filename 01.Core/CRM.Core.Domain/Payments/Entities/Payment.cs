using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.Core.Domain.Payments.Entities
{
    public class Payment:BaseEntity
    {
        public int PaymentNumber { get; set; }
        public decimal Amount { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public PayTypeEnum PaymentType { get; set; }
        public string PaymentTypeTxt { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentDateTxt { get; set; }

        [NotMapped]
        public IList<Attachment> Attachments { get; set; }

        public IList<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }

    }

}
