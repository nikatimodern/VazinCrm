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

public sealed class SepidarInvoiceBrokerPreviewDto : SepidarBrokerOptionDto
{
    public decimal Commission { get; set; }
    public decimal CurrencyRate { get; set; }
    public decimal CommissionInBaseCurrency { get; set; }
}

public sealed class PrepareExternalBrokerMappingRequest
{
    public Guid FactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
}

public sealed class SaveExternalBrokerMappingRequest
{
    public Guid FactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    // Null means skip a broker for this invoice; do not remove the salesperson's saved mapping.
    public int? BrokerPartyId { get; set; }
}
