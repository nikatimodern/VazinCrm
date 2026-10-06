using System;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseUser {
    public string UserId { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
}