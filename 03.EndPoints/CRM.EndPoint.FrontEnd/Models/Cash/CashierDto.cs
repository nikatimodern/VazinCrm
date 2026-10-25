using System.Collections.Generic;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class CashierDto {
    public ExternalSoftware ExternalSoftware { get; set; }
    public List<SelectOptionDto<string>> SelectOptions { get; set; }
}