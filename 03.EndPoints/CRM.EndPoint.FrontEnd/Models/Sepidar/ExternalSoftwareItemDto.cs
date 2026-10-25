using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public sealed class PrepareExternalItemMappingRequest
{
    public Guid PreFactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public List<int> ItemIds { get; set; }
}
