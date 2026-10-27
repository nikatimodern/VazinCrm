using System;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseTransaction : BaseEntity {
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid? FactorItemId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CRM.Core.Domain.Factors.Entities.FactorItem FactorItem { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Guid? SupplyFactorItemId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CRM.Core.Domain.Supplies.Entities.SupplyFactorItem SupplyFactorItem { get; set; }
    public WarehouseProduct WarehouseProduct { get; set; }
    public Guid WarehouseProductId { get; set; }
    public WarehouseVoucher WarehouseVoucher { get; set; }
    public Guid? WarehouseVoucherId { get; set; }
    public decimal Weight { get; set; }
    public WarehouseTransactionTypeEnum TransactionType { get; set; }
}