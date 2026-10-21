using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class ExternalSoftwarePreFactorRequest {
    public Guid PreFactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public string CustomerId { get; set; }
    public Dictionary<string, int> ItemMappings { get; set; } = new();

    public int SaleTypeId { get; set; }
    public Dictionary<string, int> StockMappings { get; set; } = new();
}