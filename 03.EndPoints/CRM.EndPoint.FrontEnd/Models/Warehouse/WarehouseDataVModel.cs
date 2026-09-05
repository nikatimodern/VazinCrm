using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Warehouse;


public class WarehousePageVModel
{
    public List<WarehouseDataVModel> Warehouses { get; set; }
    public bool IsManager { get; set; }
}

public class WarehouseDataVModel {
    public Core.Domain.Warehouses.Entities.Warehouse Warehouse { get; set; }
    public int WarehouseProductsCount { get; set; }
    public List<string> UserNames  { get; set; }
    public List<string> UserIds { get; set; }
}