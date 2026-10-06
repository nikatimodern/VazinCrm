using CRM.Framework.Domain;
using System;
using CRM.Core.Domain.Supplies.Enums;

namespace CRM.Core.Domain.Supplies.Entities {
    public class Supply : BaseEntity {
        public Guid PfId { get; set; }
        public string PfSerial { get; set; }
        public Guid? ProductCode { get; set; }
        public Guid? PfItemId { get; set; }
        public string ItemTitle { get; set; }
        public string Weight { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierTxt { get; set; }
        public decimal? UnitAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public SupplyTypeEnum SupplyType { get; set; }
        public string SupplyTypeTxt { get; set; }
    }
}