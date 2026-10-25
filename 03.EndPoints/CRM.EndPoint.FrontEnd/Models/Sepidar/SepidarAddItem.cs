using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class ExternalSoftwareAddItemRequest {
    public Guid ExternalSoftwareId { get; set; }
    public string ItemTitle { get; set; }
    public int ItemUnit { get; set; }
    public string ItemCode { get; set; }
    public decimal? ItemTaxRate { get; set; }
    public List<int> StockIds { get; set; }
}