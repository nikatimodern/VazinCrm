using System;
using System.Collections.Generic;
using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarQuotationRequest
{
    public int CustomerPartyRef { get; set; }
    public int? PartyAddressRef { get; set; }
    public string CustomerName { get; set; }
    public PreFactor PreFactor { get; set; }
    public List<SepidarQuotationItemRequest> Items { get; set; } = new();
}

public class SepidarQuotationItemRequest
{
    public int ItemRef { get; set; }
    public int StockRef { get; set; }
    public decimal Quantity { get; set; }
    public decimal Fee { get; set; }
    public decimal Discount { get; set; }
    public decimal Addition { get; set; }
    public decimal Tax { get; set; }
    public decimal Duty { get; set; }
    public string Description { get; set; }
}