using System;
using CRM.Core.Domain.Orders.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class CashOrderHead : JunctionBaseEntity {
    public Guid CashId { get; set; }
    public Guid OrderHeadId { get; set; }

    public Cash Cash { get; set; }
    public OrderHead OrderHead { get; set; }
}