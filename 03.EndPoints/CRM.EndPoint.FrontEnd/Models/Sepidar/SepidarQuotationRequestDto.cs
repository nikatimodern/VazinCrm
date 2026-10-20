using System;
using System.Collections.Generic;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Parties;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarQuotationRequest
{
    public required PreFactor PreFactor { get; set; }
    public required List<SepidarQuotationItemRequest> Items { get; set; }

    public required SepidarPartyDto Party { get; set; }
}

public class SepidarQuotationItemRequest
{
    public required PreFactorItem PreFactorItem { get; set; }
    public int ItemRef { get; set; }
}