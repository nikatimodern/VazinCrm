using System;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.Core.Domain.Cashes.Entities;

public class CashierExternalSoftware {
    public Guid Id { get; set; }

    public Guid CashierId { get; set; }
    public Cashier Cashier { get; set; }

    public Guid ExternalSoftwareId { get; set; }
    public ExternalSoftware ExternalSoftware { get; set; }
    
    public string TafsiliCode { get; set; }
}