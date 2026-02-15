using System.Collections.Generic;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class SupplyFactorVModel {
    public SupplyFactor SupplyFactor { get; set; }
    public List<int> OrderSerials { get; set; }
}