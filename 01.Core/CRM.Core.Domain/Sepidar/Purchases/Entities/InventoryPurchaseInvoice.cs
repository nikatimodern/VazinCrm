using System;

namespace CRM.Core.Domain.Sepidar.Purchases.Entities;

public sealed class InventoryPurchaseInvoice
{
    public int InventoryPurchaseInvoiceID { get; set; }
    public int VendorDLRef { get; set; }
    public int Number { get; set; }
    public int? InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public int CurrencyRef { get; set; }
    public decimal CurrencyRate { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? TotalPriceInBaseCurrency { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalDuty { get; set; }
    public decimal? TotalTransportPrice { get; set; }
    public decimal? TotalNetPrice { get; set; }
    public decimal? TotalNetPriceInBaseCurrency { get; set; }
    public decimal? TotalWithHoldingTaxAmount { get; set; }
    public decimal? TotalInsuranceAmount { get; set; }
    public int FiscalYearRef { get; set; }
    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int Version { get; set; }
    public int? PaymentHeaderRef { get; set; }
    public int Type { get; set; }
    public int? AccountingVoucherRef { get; set; }
    public int? SLAccountRef { get; set; }
    public int? CostCenterRef { get; set; }
    public int? AssetAcquisitionPurchaseSLRef { get; set; }
    public int? PartyAddressRef { get; set; }
    public string Description { get; set; }
    public string Description_En { get; set; }
    public int? PurchasingAgentPartyRef { get; set; }
    public int? PurchaseOrderRef { get; set; }
}
