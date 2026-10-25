using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class ExternalSoftwarePreFactorRequest {
    public Guid PreFactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public int ExternalCustomerId { get; set; }
    public int SaleTypeId { get; set; }
    public List<ExternalPreFactorItemMapping> ItemMappings { get; set; } = [];
}

public class ExternalPreFactorItemMapping {
    public Guid PreFactorItemId { get; set; }
    public int ExternalItemId { get; set; }
    public int ExternalStockId { get; set; }
}