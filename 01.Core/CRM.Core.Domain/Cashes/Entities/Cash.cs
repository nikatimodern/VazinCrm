using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Cashes.Enums;

namespace CRM.Core.Domain.Cashes.Entities {
    public class Cash : BaseEntity {
        public int Serial { get; set; }

        public Guid CostCenterId { get; set; } // مرکز هزینه
        public string CostCenterTxt { get; set; }
        public BaseCenterCash CostCenter { get; set; }

        public Guid CostTitleId { get; set; } //عنوان هزینه
        public string CostTitleTxt { get; set; }
        public BaseCash CostTitle { get; set; }

        public string CostDescription { get; set; }
        public decimal Amount { get; set; } //مبلغ
        public string CostDate { get; set; } //تاریخ هزینه
        public DateTime CostDateMiladi { get; set; } //تاریخ هزینه
        public CashStatusEnum Status { get; set; } = CashStatusEnum.ثبت_موقت; //وضعیت
        public string StatusTxt { get; set; }

        public decimal Fee { get; set; } // کارمزد
        public decimal TotalAmount => Amount + Fee; // مجموع مبلغ و کارمزد
        public string ReferenceNumber { get; set; } // شماره پیگیری پرداخت
        public int? PaymentVoucherNumber { get; set; } // شماره اعلامیه پرداخت

        public Accountancy Accountancy { get; set; }
        [NotMapped] public IList<Attachment> Attachments { get; set; }

        public Guid? CashGroupId { get; set; }
        public CashGroup CashGroup { get; set; }

        public BankAccount BankAccount { get; set; }
        public Guid? BankAccountId { get; set; }
        public string BankAccountTxt { get; set; }


        public IList<CashExternalSoftware> CashExternalSoftwares { get; set; }
        public ICollection<CashOrderHead> CashOrderHeads { get; set; }

        public Cashier Cashier { get; set; } 
        public Guid? CashierId { get; set; }
    }
}