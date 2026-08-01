using System;
using System.Collections.Generic;
using System.Text;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Accountancies.Entities {
    public class BankAccount : BaseEntity {
        public string ShebaNumber { get; set; }
        public string AccountNumber { get; set; }
        public string CartNumber { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public bool Status { get; set; } = true;
        public byte OfficialType { get; set; }
        public bool Active { get; set; }
        public int? AccountCode { get; set; } // شماره تفضیل حساب

        public BankAccountTypeEnum BankAccountType { get; set; }
        public string BankAccountTypeTxt { get; set; }

        public Guid? CustomerId { get; set; }
        public int? CustomerCode { get; set; }


        public List<Accountancy> Accountancies { get; set; }
        public List<Checkbook> Checkbooks { get; set; }
        public List<Cash> Cashes { get; set; }
    }

    public enum BankAccountTypeEnum {
        شرکت = 1,
        تامین_کننده = 2,
        تنخواه_دار = 3
    }
}