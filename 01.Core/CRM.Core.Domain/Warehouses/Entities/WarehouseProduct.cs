using System;
using System.Collections.Generic;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseProduct : BaseEntity {
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }

    public Product Product { get; set; }
    public Guid? ProductCode { get; set; }
    public string ProductSmallName { get; set; }
    public string ProductDescription { get; set; }

    public UnitTypeEnums Unit { get; set; }
    public string UnitTxt { get; set; }
    

    public IList<WarehouseTransaction> Transactions { get; set; }
    public IList<WarehouseProductVoucher> WarehouseProductVouchers { get; set; }
}