using System;
using System.Collections.Generic;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Warehouse;

public class WarehouseVoucherListVModel {
    public IList<WarehouseVoucher> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public int DisplayFrom => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int DisplayTo => Math.Min(Page * PageSize, TotalCount);


    public string Search { get; set; }
    public WarehouseVoucherStatusEnum? Status { get; set; }
    public IList<WarehouseVModel> Warehouses { get; set; } = [];
}