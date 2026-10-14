using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities {
    public class Cashier : BaseEntity {
        public string Title { get; set; }
        public Guid UserId { get; set; }

        public string UserTxt { get; set; }

        public BankAccount BankAccount { get; set; }
        public Guid? BankAccountId { get; set; }


        public IList<CashierExternalSoftware> CashierExternalSoftwares { get; set; }
        public IList<Cash> Cashes { get; set; }
    }
}