using System;
using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Accountancies.Entities {
    public class BankAccount : BaseEntity {
        public string ShebaNumber { get; set; }
        public string AccountNumber { get; set; }
        public string CartNumber { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        //public bool Status { get; set; } = true;
        public byte OfficialType { get; set; }
        public bool Active { get; set; }

        public BankAccountTypeEnum BankAccountType { get; set; }
        public string BankAccountTypeTxt { get; set; }

        public Guid? CustomerId { get; set; }
        public Customer Customer { get; set; }


        public List<Accountancy> Accountancies { get; set; }
        public List<Checkbook> Checkbooks { get; set; }
        public List<Cash> Cashes { get; set; }

        public List<Cashier> Cashiers { get; set; }
    }
}