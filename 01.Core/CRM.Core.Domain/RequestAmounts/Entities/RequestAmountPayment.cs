using System;
using CRM.Core.Domain.Payments.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.RequestAmounts.Entities;

public class RequestAmountPayment : JunctionBaseEntity {
    public Guid RequestAmountId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }

    public RequestAmount RequestAmount { get; set; }
    public Payment Payment { get; set; }
}