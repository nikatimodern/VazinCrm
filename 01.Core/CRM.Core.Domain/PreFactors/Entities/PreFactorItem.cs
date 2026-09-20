using CRM.Core.Domain.Supplies.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using CRM.Core.Domain.PreFactors.Enum;

namespace CRM.Core.Domain.PreFactors.Entities {
    public class PreFactorItem : BaseEntity {
        public int Row { get; set; }
        public string Product { get; set; }
        public string WeightCount { get; set; }
        public int Unit { get; set; }
        public string UnitTxt { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }

        public decimal? TaminUnitAmount { get; set; }
        public decimal? TaminTotalAmount { get; set; }

        public Guid? ProductGroup { get; set; }
        public string ProductGroupTxt { get; set; }

        public Guid? ProductCode { get; set; }
        public string ProductSmallName { get; set; }
        public bool IsInProduction { get; set; } = true;
        public byte? Printable { get; set; } = 0;

        //public PreFactor PreFactor { get; set; }
        public Guid PrefactorId { get; set; }

        public List<Supply> supplies { get; set; }
        //public Guid? SupplierId { get; set; }
        //public string SupplierName { get; set; }
    }
}