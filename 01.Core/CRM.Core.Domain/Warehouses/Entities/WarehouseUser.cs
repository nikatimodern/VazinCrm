using System;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseUser {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
}