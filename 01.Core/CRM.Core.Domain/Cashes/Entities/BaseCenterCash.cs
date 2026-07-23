using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class BaseCenterCash : BaseEntity {
    public string Code { get; set; }
    public string Title { get; set; }

    public ICollection<Cash> Cashes { get; set; }
}