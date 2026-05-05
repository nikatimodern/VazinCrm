using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.RequestAmounts.Entities;

public class RequestAmountPayment : JunctionBaseEntity {
    public Guid RequestAmountId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
}