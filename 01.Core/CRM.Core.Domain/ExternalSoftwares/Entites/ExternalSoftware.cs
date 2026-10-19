using System;
using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Framework;

namespace CRM.Core.Domain.ExternalSoftwares.Entites;

public class ExternalSoftware {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public int Code { get; set; }
    public SoftwareTypeEnum SoftwareType { get; set; }
    public string SoftwareTypeTxt { get; set; }

    public Guid? SoftwareBaseItemId { get; set; }
    public string SoftwareBaseItemTxt { get; set; }

    public string DatabaseName { get; set; }
    public string DatabaseUserName { get; set; }
    public string DatabasePassword { get; set; }
    public string DatabaseIp { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.Now;
    public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();

    public IList<ExternalMapping> ExternalMappings { get; set; }
    public IList<ExternalSoftwareSetting> ExternalSoftwareSettings { get; set; }

    public IList<CashierExternalSoftware> CashierExternalSoftwares { get; set; }
    public IList<CashExternalSoftware> CashExternalSoftwares { get; set; }
}