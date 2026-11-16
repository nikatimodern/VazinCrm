namespace CRM.Core.Domain.Sepidar.Purchases.Entities;

public sealed class InventoryPurchaseInvoiceItem
{
    public int InventoryPurchaseInvoiceItemID { get; set; }
    public int InventoryPurchaseInvoiceRef { get; set; }
    public int RowNumber { get; set; }
    public int ItemRef { get; set; }
    public decimal Quantity { get; set; }
    public decimal? SecondaryQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal? RemainingSecondaryQuantity { get; set; }
    public decimal CurrencyRate { get; set; }
    public decimal Fee { get; set; }
    public decimal? FeeInBaseCurrency { get; set; }
    public decimal Price { get; set; }
    public decimal? PriceInBaseCurrency { get; set; }
    public decimal? Discount { get; set; }
    public decimal? DiscountInBaseCurrentcy { get; set; }
    public decimal? Addition { get; set; }
    public decimal? AdditionInBaseCurrency { get; set; }
    public decimal? Tax { get; set; }
    public decimal? TaxInBaseCurrency { get; set; }
    public decimal? Duty { get; set; }
    public decimal? DutyInBaseCurrency { get; set; }
    public decimal? TransportPrice { get; set; }
    public decimal? TransportPriceInBaseCurrency { get; private set; }
    public decimal? TransportTax { get; set; }
    public decimal? TransportTaxInBaseCurrency { get; private set; }
    public decimal? TransportDuty { get; set; }
    public decimal? TransportDutyInBaseCurrency { get; private set; }
    public string TransportDescription { get; set; }
    public decimal? NetPriceInBaseCurrency { get; set; }
    public string Description { get; set; }
    public string Description_En { get; set; }
    public int Version { get; set; }
    public int? CostServiceAccountSLRef { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public decimal? InsuranceAmountInBaseCurrency { get; set; }
    public decimal? WithHoldingTaxAmount { get; set; }
    public decimal? WithHoldingTaxAmountInBaseCurrency { get; set; }
    public int? TracingRef { get; set; }
    public int? PurchaseOrderItemRef { get; set; }
    public decimal? NetPrice { get; private set; }
}
