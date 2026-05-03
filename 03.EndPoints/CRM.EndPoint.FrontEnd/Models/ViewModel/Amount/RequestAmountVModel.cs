using System.Collections.Generic;
using System.Linq;
using CRM.Core.Domain.RequestAmounts.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Amount;

public class RequestAmountVModel {
    public RequestAmount RequestAmount { get; set; }
    public List<string> SupplyFactorSerials { get; set; }

    public List<RequestAmountOrderHeadDataVModel> OrderHeadsData { get; set; }
    public List<int> OrderNumbers => OrderHeadsData?.Select(x => x.OrderNumber).ToList();
    public List<string> CustomerNames => OrderHeadsData?.Select(x => x.CustomerTxt).ToList();
}

public class RequestAmountOrderHeadDataVModel {
    public int OrderNumber { get; set; }
    public string CustomerTxt { get; set; }
}