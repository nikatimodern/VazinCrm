using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Payment
{
    public class PaymentVm
    {
        public Guid Id { get; set; }

        public Guid? CustomerId { get; set; }
        public Guid? PFactorId { get; set; }
        public Guid? FactorId { get; set; }

        public int? SerialFactor { get; set; }
        public int? SerialPfactor { get; set; }

        public int InvoiceType { get; set; }//فاکتور یا پیش فاکتور
        public int PayType { get; set; }//نوع واریزی نقدی ، چکی ، اعتباری
        public AcEnum AccType { get; set; } //واریزی _ 1بدهکاری _ 2برگشتی


        public string PayDate { get; set; }
        public string VosolDate { get; set; }   
        public string ReciveDate { get; set; }   
        public decimal ReciveAmount { get; set; }

        public string BankAccountId { get; set; }
        public string BankAccountTxt { get; set; }
        public string FromBankAccountTxt { get; set; }
        public string Beneficiary { get; set; }

        public bool? Accepted { get; set; }
        public AccStatusEnum Status { get; set; } = AccStatusEnum.ثبت_اولیه;
        public string StatusTxt { get; set; } = nameof(AccStatusEnum.ثبت_اولیه).Replace("_", " ");
        public bool Vosoled { get; set; }

        public Guid? ParentId { get; set; }
        public string ToName { get; set; }

    }
}
