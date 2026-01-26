using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class SuppliesProfitDtVModel {
    public Guid PfId { get; set; }
    public Guid DealId { get; set; }
    public Guid? CustomerId { get; set; }
    public string DealTitle { get; set; }
    public int? CustomerCode { get; set; }
    public string CustomerName { get; set; }
    public string PfDate { get; set; }
    public DateTime? PfDateM { get; set; }
    public string DealDate { get; set; }
    public DateTime? DealDateM { get; set; }
    public string FDate { get; set; }
    public DateTime? FDateM { get; set; }
    public int PfSerialNumber { get; set; }
    public int? DealSerialNumber { get; set; }
    public string UserCreatorDeal { get; set; }
    public string UserCreatorDealId { get; set; }
    public string UserTaminTxt { get; set; }
    public decimal TotalSuppliesAmount { get; set; }
    public decimal TotalFactorsAmount { get; set; }
    public decimal Profit { get; set; }
    public decimal? ProfitPercent { get; set; }
}