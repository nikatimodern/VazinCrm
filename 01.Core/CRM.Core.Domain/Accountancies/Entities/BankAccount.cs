using System;
using System.Collections.Generic;
using System.Text;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Accountancies.Entities
{
    public class BankAccount:BaseEntity
    {
        public string ShebaNumber { get; set; }
        public string AccountNumber { get; set; }
        public string CartNumber { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public bool Status { get; set; } = true;
        public byte OfficialType { get; set; }
        public bool Active { get; set; }

        public List<Accountancy> Accountancies { get; set; }
    }
}
