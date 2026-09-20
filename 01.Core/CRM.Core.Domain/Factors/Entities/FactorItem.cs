using CRM.Framework.Domain;
using System;
using CRM.Core.Domain.PreFactors.Enum;

namespace CRM.Core.Domain.Factors.Entities
{
    public class FactorItem : BaseEntity
    {
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
        //public PreFactor PreFactor { get; set; }
        public Guid factorId { get; set; }
        public Guid? ProductCode { get; set; }
        public string ProductSmallName { get; set; }


        public byte? Printable { get; set; }
        public Guid? PfItemId { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; }



    }
}
