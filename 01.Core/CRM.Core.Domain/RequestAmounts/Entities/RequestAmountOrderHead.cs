using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.RequestAmounts.Entities;

public class RequestAmountOrderHead : JunctionBaseEntity {
    public Guid RequestAmountId { get; set; }
    public Guid OrderHeadId { get; set; }
}