using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using CRM.Core.Domain.RequestAmounts.Entities;

namespace CRM.Core.Domain.Orders.Entities {
    public class OrderHead : BaseEntity {
        public int OrderNumber { get; set; }
        public string Neme { get; set; }
        public string Tel { get; set; }
        public string OrderDate { get; set; }
        public DateTime? OrderDateMildi { get; set; }
        public string Description { get; set; }

        //public Customer Customer { get; set; }
        public Guid? Customer { get; set; }
        public string CustomerTxt { get; set; }
        public int? CustomerCode { get; set; }

        public Guid? Prefactor { get; set; }
        public int? PrefactorSer { get; set; }

        public OrderHeadStatusEnum? Status { get; set; } = OrderHeadStatusEnum.ثبت_اولیه;


        public IList<Order> Orders { get; set; }


        public Guid? UserTaminId { get; set; }
        public string UserTaminTxt { get; set; }

        public Guid? DealId { get; set; }
        public int? DealSerialNumber { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierTxt { get; set; }

        public IList<SupplyFactorOrderHead> SupplyFactorOrderHeads { get; set; }
        public IList<RequestAmountOrderHead> RequestAmountOrderHeads { get; set; }
    }
}