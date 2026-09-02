using System;
using CRM.Core.Domain.Warehouses.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Warehouse;

public class WarehouseTransactionVModel {
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? WarehouseProductId { get; set; }
    public Guid ProductCode { get; set; }
    public string ProductSmallName { get; set; }
    public string ProductDescription { get; set; }
    public UnitTypeEnums Unit { get; set; }
    public decimal Weight { get; set; }
    public WarehouseTransactionTypeEnum TransactionType { get; set; }
}