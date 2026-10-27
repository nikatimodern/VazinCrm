using System;

namespace CRM.Core.Domain.Supplies.Models;
public class SupplyFactorAllocationRequest
{
    public Guid Id { get; set; }
    public Guid SupplyFactorItemId { get; set; }
    public Guid? FactorItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SourceWarehouseProductId { get; set; }
    public decimal Weight { get; set; }
}
