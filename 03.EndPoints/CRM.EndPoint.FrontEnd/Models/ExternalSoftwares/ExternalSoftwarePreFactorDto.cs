using System;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class ExternalSoftwarePreFactorDto
{
    public Guid MappingId { get; set; }
    public bool IsMissing { get; set; }
    public bool CanDeleteMissing { get; set; }
    public bool CanMaintainDestination { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public string ExternalSoftwareTitle { get; set; } = string.Empty;

    public string PreFactorNumber { get; set; } = string.Empty;
    public string ExternalPreFactorNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public int InvoicesCount { get; set; }

    public string ViewUrl { get; set; } = string.Empty;
    public string CreationDate { get; set; }
}
