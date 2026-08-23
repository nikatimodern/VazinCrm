using System.Collections.Generic;
using RequestAmountEntity = CRM.Core.Domain.RequestAmounts.Entities.RequestAmount;

namespace CRM.EndPoint.FrontEnd.Models.RequestAmount;

public class RequestAmountVModel {
    public RequestAmountEntity RequestAmount { get; set; }
    public List<string> SupplyFactorSerials { get; set; }
    public List<int> OrderHeadSerials { get; set; }
    public List<string> CustomerNames { get; set; }
    public decimal TotalPaymentAmount { get; set; }
    public decimal SupplierDebt { get; set; }
}