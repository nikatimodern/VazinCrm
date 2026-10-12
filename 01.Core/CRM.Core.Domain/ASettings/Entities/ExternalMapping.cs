using System;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Framework;

namespace CRM.Core.Domain.ASettings.Entities;

public class ExternalMapping {
    public Guid Id { get; set; }
    public string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public ExternalSoftware ExternalSoftware { get; set; }

    public ExternalMappingTypeEnum MapType { get; set; }
    public string MapTypeTxt { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.Now;
    public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();
}