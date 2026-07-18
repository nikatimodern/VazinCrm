using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class CashGroup : BaseEntity {
    public int GroupNumber { get; set; }
    public ICollection<Cash> Cashes { get; set; } = [];
}