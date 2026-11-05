using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Supplies.Models
{

    public class ReceivablesSummary
    {
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }

        public long TotalFactorCount { get; set; }
        public decimal TotalFactorSum { get; set; }

        public long TotalAccCount { get; set; }
        public decimal TotalAccSum { get; set; }

        public long TotalRetAccBackCount { get; set; }
        public decimal TotalRetAccBackSum { get; set; }

        public decimal TotalDebitSum { get; set; }
        public decimal TotalReceivableSum { get; set; }
        public decimal TotalDebtSum { get; set; }
    }

}
