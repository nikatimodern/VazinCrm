using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Supplies.Entities
{
    public class SupplyFactor:BaseEntity
    {
        public string FactorSerial { get; set; }//شماره فاکتور صادر شده توسط تامیین کننده
        public decimal Amount { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierTxt { get; set; }
        public string FactorFileUrl { get; set; }
        public DateTime FactorDate { get; set; }
        public string  FactorDateTxt { get; set; }

        public IList<SupplyOrderHead> OrderHeads { get; set; }
    }
}
