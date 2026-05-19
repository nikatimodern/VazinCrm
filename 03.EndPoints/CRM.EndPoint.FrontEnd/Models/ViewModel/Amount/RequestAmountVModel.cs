using System.Collections.Generic;
using CRM.Core.Domain.RequestAmounts.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Amount;

public class RequestAmountVModel {
    public RequestAmount RequestAmount { get; set; }
    public List<string> SupplyFactorSerials { get; set; }
    public List<int> OrderHeadSerials { get; set; }
    public List<string> CustomerNames { get; set; }
    public decimal TotalPaymentAmount { get; set; }
}