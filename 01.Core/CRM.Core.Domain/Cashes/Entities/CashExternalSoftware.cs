using System;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.Core.Domain.Cashes.Entities;

public class CashExternalSoftware {
    public Guid Id { get; set; }

    public Guid CashId { get; set; }
    public Cash Cash { get; set; }

    public Guid ExternalSoftwareId { get; set; }
    public ExternalSoftware ExternalSoftware { get; set; }
    
    public string TafsiliCode { get; set; }
}