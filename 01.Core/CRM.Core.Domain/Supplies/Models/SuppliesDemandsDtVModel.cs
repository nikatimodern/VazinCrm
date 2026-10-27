using System;

namespace CRM.Core.Domain.Supplies.Models;
public class SuppliesDemandsDtVModel
{
    public Guid DealId { get; set; }
    public Guid? CustomerId { get; set; }
    public string DealTitle { get; set; }
    public string CustomerName { get; set; }
    public int? CustomerCode { get; set; }
    public int? DealSerialNumber { get; set; }
    public string DealDate { get; set; }
    public DateTime? DealDateM { get; set; }
    public decimal TotalFactorsAmount { get; set; }
    public decimal TotalAccAmount { get; set; }
    public decimal Demand { get; set; }
    public int PfSerialNumber { get; set; }
    public string UserCreatorDeal { get; set; }
    public string UserCreatorDealId { get; set; }
}
