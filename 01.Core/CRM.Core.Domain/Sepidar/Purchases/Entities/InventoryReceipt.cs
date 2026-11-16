using System;

namespace CRM.Core.Domain.Sepidar.Purchases.Entities;

public sealed class InventoryReceipt
{
    public int InventoryReceiptID { get; set; }
    public bool IsReturn { get; set; }
    public int Type { get; set; }
    public int PurchaseType { get; set; }
    public int StockRef { get; set; }
    public int? DelivererDLRef { get; set; }
    public int? SLAccountRef { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public int? AccountingVoucherRef { get; set; }
    public int? PaymentHeaderRef { get; set; }
    public int? TransportBrokerSLAccountRef { get; set; }
    public int? TransporterDLRef { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalDuty { get; set; }
    public decimal? TotalTransportPrice { get; set; }
    public decimal? TotalNetPrice { get; set; }
    public int FiscalYearRef { get; set; }
    public int CreatorForm { get; set; }
    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int Version { get; set; }
    public int? BasePurchaseInvoiceRef { get; set; }
    public int? BaseInventoryDeliveryRef { get; set; }
    public decimal? TotalReturnedPrice { get; set; }
    public decimal? TotalReturnedNetPrice { get; set; }
    public int? BaseImportPurchaseInvoiceRef { get; set; }
    public string Description { get; set; }
    public decimal? TotalOtherCost { get; set; }
    public bool IsWastage { get; set; }
}
