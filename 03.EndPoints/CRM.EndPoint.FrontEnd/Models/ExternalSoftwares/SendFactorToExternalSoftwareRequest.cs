using System;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class SendFactorToExternalSoftwareRequest
{
    public Guid FactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
}
