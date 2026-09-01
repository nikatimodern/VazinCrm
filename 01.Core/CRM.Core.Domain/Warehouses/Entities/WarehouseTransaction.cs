using System;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseTransaction : BaseEntity {
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public string ProductTxt { get; set; }
    public UnitTypeEnums Unit { get; set; }
    public string UnitTxt { get; set; }
    public decimal Weight { get; set; }
    public WarehouseTransactionTypeEnum TransactionType { get; set; }
}