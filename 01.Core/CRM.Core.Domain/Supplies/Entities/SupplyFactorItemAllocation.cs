using System;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.Domain.Supplies.Entities;

public class SupplyFactorItemAllocation {
    public Guid Id { get; set; }
    public Guid SupplyFactorItemId { get; set; }
    public Guid? FactorItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public CRM.Core.Domain.Warehouses.Entities.Warehouse Warehouse { get; set; }
    public decimal Weight { get; set; }
    public SupplyFactorItem SupplyFactorItem { get; set; }
    public FactorItem FactorItem { get; set; }
}
