using CRM.Core.Domain.PreFactors.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public enum SupplyTypeEnum {
        هزینه_تولید = 1,
        هزینه_اضافی = 2,
        ضایعات = 3
    }
}