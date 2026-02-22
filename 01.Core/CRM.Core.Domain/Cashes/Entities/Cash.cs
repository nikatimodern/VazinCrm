using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Cashes.Entities {
    public class Cash : BaseEntity {
        public int Serial { get; set; }
        public Guid CashierId { get; set; }
        public string CashierTxt { get; set; }
        public string CostTitle { get; set; } //عنوان هزینه
        public string CostCenter { get; set; } //مرکز هزینه
        public decimal Amount { get; set; } //مبلغ
        public string CostDate { get; set; } //تاریخ هزینه
        public DateTime CostDateMiladi { get; set; } //تاریخ هزینه
        public CashStatusEnum Status { get; set; } = CashStatusEnum.ثبت_موقت; //وضعیت
        public string StatusTxt { get; set; }
    }


    public enum CashStatusEnum {
        ثبت_موقت,
        ارسال_به_حسابداری
    }
}