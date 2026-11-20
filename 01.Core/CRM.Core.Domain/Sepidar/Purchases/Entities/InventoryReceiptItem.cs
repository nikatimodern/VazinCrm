namespace CRM.Core.Domain.Sepidar.Purchases.Entities;

public sealed class InventoryReceiptItem
{
    public int InventoryReceiptItemID { get; set; }
    public int InventoryReceiptRef { get; set; }
    public bool IsReturn { get; set; }
    public int RowNumber { get; set; }
    public int? Base { get; set; }
    public int? ReturnBase { get; set; }
    public int ItemRef { get; set; }
    public int? TracingRef { get; set; }
    public decimal Quantity { get; set; }
    public decimal? SecondaryQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal? RemainingSecondaryQuantity { get; set; }
    public int? CurrencyRef { get; set; }
    public decimal? CurrencyRate { get; set; }
    public decimal? CurrencyValue { get; set; }
    public decimal? Price { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Duty { get; set; }
    public decimal? TransportPrice { get; set; }
    public decimal? TransportTax { get; set; }
    public decimal? TransportDuty { get; set; }
    public string TransportDescription { get; set; }
    public string Description { get; set; }
    public string Description_En { get; set; }
    public int Version { get; set; }
    public int? BasePurchaseInvoiceItemRef { get; set; }
    public string ParityCheck { get; set; }
    public int? ProductOrderRef { get; set; }
    public int? InventoryDeliveryItemRef { get; set; }
    public int? WeighingRef { get; set; }
    public decimal? TaxCurrencyValue { get; set; }
    public decimal? DutyCurrencyValue { get; set; }
    public decimal? ReturnedPrice { get; set; }
    public decimal? Fee { get; private set; }
    public decimal? ReturnedFee { get; private set; }
    public int? OtherCostsAmount { get; set; }
    public int? ServiceInventoryPurchaseInvoiceRef { get; set; }
    public int? ImportOrderFinalFee { get; set; }
    public int? BaseImportPurchaseInvoiceItemRef { get; set; }
    public decimal? AllotmenedOtherCostInBaseCurrency { get; set; }
    public decimal? NetPrice { get; private set; }
    public decimal? ReturnedNetPrice { get; private set; }
}
