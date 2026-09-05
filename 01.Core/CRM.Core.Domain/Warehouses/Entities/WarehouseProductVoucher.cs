using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseProductVoucher : JunctionBaseEntity {
    public Guid WarehouseProductId { get; set; }
    public WarehouseProduct WarehouseProduct { get; set; }
    
    public Guid WarehouseVoucherId { get; set; }
    public WarehouseVoucher WarehouseVoucher { get; set; }
    
    public decimal Weight { get; set; }
}