using System.Collections.Generic;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.Core.Domain.Supplies.Models;
public class SupplyFactorVModel
{
    public bool IsAllocationComplete { get; set; }
    public SupplyFactor SupplyFactor { get; set; }
    public List<int> OrderSerials { get; set; }
}
