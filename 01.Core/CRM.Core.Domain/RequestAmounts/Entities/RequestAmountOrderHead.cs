using System;
using CRM.Core.Domain.Orders.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.RequestAmounts.Entities;

public class RequestAmountOrderHead : JunctionBaseEntity {
    public Guid RequestAmountId { get; set; }
    public Guid OrderHeadId { get; set; }

    public RequestAmount RequestAmount { get; set; }
    public OrderHead OrderHead { get; set; }
}