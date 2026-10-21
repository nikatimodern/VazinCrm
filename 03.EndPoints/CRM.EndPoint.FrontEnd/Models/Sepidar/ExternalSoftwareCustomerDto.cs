using System;
using System.Collections.Generic;
using CRM.Core.Domain.Sepidar.SaleTypes;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public sealed class ExternalSoftwareCustomerSelectionDto {
    public Guid ExternalSoftwareId { get; set; }
    public string ExternalSoftwareTitle { get; set; } = string.Empty;

    public Guid CrmCustomerId { get; set; }
    public string CrmCustomerTitle { get; set; } = string.Empty;

    public int? MappedExternalCustomerId { get; set; }

    public List<SepidarPartyDto> Customers { get; set; } = new();
}

public sealed class SaveExternalCustomerMappingRequest {
    public Guid PreFactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public int ExternalCustomerId { get; set; }
}