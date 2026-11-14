using System;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarBrokerOptionDto
{
    public int PartyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DlCode { get; set; } = string.Empty;
    public int DlRef { get; set; }
    public decimal CommissionRate { get; set; }
}

// Lightweight person information for the optional broker switch, even before it is enabled.
// This is display-only; invoice registration still validates IsBroker and the active DL.
public sealed class SepidarInvoiceBrokerHintDto
{
    public string FullName { get; set; } = string.Empty;
    public string DlCode { get; set; } = string.Empty;
    public bool IsBroker { get; set; }
    public bool HasActiveDetailedAccount { get; set; }
}

public sealed class SepidarInvoiceBrokerPreviewDto : SepidarBrokerOptionDto
{
    public decimal Commission { get; set; }
    public decimal CurrencyRate { get; set; }
    public decimal CommissionInBaseCurrency { get; set; }
}

