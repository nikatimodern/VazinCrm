using CRM.Framework.Domain;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Cashes.Entities {
    public class Cashier : BaseEntity
    {
        public IList<CashierUser> CashierUsers { get; set; }
        public string Title { get; set; }
        public IList<Cash> Cashes { get; set; }
    }
}