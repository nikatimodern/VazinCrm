using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Warehouse;

public class WarehouseVModel {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public bool IsMain { get; set; }
    public List<string> UserIds { get; set; } = [];
}