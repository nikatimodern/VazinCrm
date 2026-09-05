using System;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseTransaction : BaseEntity {
    public Guid WarehouseProductId { get; set; }
    public WarehouseProduct WarehouseProduct { get; set; }
    public decimal Weight { get; set; }
    public WarehouseTransactionTypeEnum TransactionType { get; set; }
}