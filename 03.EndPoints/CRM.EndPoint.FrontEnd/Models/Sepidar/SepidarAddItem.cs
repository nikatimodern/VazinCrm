using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class ExternalSoftwareAddItemRequest {
    public Guid ExternalSoftwareId { get; set; }
    public string itemTitle { get; set; }
    public int itemUnit { get; set; }
    public List<int> stockIds { get; set; }
}