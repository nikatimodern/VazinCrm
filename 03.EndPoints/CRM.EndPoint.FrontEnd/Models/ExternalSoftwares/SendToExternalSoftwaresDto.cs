using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class SendToExternalSoftwaresDto {
    public IList<Guid> Ids { get; set; }
    public IList<Guid> ExternalSoftwareIds { get; set; }
}