using System;
using CRM.Core.Domain.ExternalSoftwares.Enums;

namespace CRM.Core.Domain.ExternalSoftwares.Entites;

public class ExternalSoftwareSetting {
    public Guid Id { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public string SettingValue { get; set; }
    public string SettingName { get; set; }
    public ExternalSoftwareSettingTypeEnum SettingNameCode { get; set; }
}