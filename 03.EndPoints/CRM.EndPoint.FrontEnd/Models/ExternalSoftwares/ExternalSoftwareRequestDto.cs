using System;
using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Enums;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class ExternalSoftwareRequestDto {
    public List<Guid> EntityIds { get; set; } = [];
    public ExternalMappingTypeEnum MapType { get; set; }
}