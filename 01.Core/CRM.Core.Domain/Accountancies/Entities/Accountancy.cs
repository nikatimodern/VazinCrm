using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.PreFactors.Enum;

namespace CRM.Core.Domain.Accountancies.Entities {
    public class Accountancy : BaseEntity {
        public InvoiceTypeEnum InvoiceType { get; set; } //فاکتور یا پیش فاکتور
        public string InvoiceTypeTxt { get; set; }

        public PayTypeEnum PayType { get; set; } //نوع واریزی 1-نقدی ، 2-چکی ، 3-اعتباری
        public string PayTypeTxt { get; set; }

        public AcEnum AccType { get; set; } // بدهکاری، واریزی، برگشتی
        public string AccTypeTxt { get; set; }

        public decimal? credit { get; set; }
        public decimal? debit { get; set; }
        public decimal? BackWard { get; set; }


        public decimal? discount { get; set; }
        public decimal? tax { get; set; }
        public int? taxPercent { get; set; }

        public DateTime? date { get; set; }
        public string dateSh { get; set; }

        public DateTime? VosolDateM { get; set; }
        public string VosolDate { get; set; } //تاریخ وصول پرداختی های به صورت چک یا اعتباری یا نقدی در نقدی همان تاریخ پرداخت هست

        public DateTime? ReciveDateM { get; set; }
        public string ReciveDate { get; set; } //تاریخ ثبت یا دریافت پرداختی های به صورت چک


        public Guid? pfid { get; set; }
        public long? pfSerialNumber { get; set; }

        public Guid? fid { get; set; }
        public long? fSerialNumber { get; set; }

        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int? CustomerCode { get; set; }
        public Guid? DealId { get; set; }
        public int? DealSerialNumber { get; set; }
        public Deal Deal { get; set; }
        [NotMapped] public int SerialNum { get; set; }

        [NotMapped] public int Row { get; set; }
        public bool FirstAcc { get; set; }
        public Guid? BankAccountId { get; set; }
        public string BankAccountTxt { get; set; }
        public string FromBankAccountTxt { get; set; }
        public string ToName { get; set; }
        public string Beneficiary { get; set; } // ذینفع

        public bool Vosoled { get; set; } // آیا چک یا اعتباری وصول شده است یا خیر
        public bool? Accepted { get; set; }
        public string AcceptedUser { get; set; }
        public string AcceptedUserId { get; set; }

        private AccStatusEnum? _status = AccStatusEnum.ثبت_اولیه;

        public AccStatusEnum? Status {
            get => _status;
            set {
                _status = value;
                StatusTxt = value?.ToString().Replace("_", " ");
            }
        }

        public string StatusTxt { get; private set; }

        public Guid? ParentId { get; set; }

        public CurrencyTypeEnum CurrencyType { get; set; }
        public string CurrencyTypeTxt { get; set; }
        public bool Wallet { get; set; }

        [NotMapped] public decimal ReciveAmount { get; set; }

        public Guid? CashId { get; set; }
        public Guid? CashierId { get; set; }
        public Guid? RequestAmountId { get; set; }
    }
}