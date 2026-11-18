using System;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public sealed class DeleteMissingExternalFinancialMappingRequest
{
    public Guid SourceId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public Guid MappingId { get; set; }
}
