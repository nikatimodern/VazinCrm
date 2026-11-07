using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Invoices.Entities;

// Write model for SLS.InvoiceItem. Do not remap the existing vwInvoiceItem read model.
[Table("InvoiceItem", Schema = "SLS")]
public class SalesInvoiceItemRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int InvoiceItemID { get; set; }
    public int InvoiceRef { get; set; }
    public int RowID { get; set; }
    public int ItemRef { get; set; }
    public int? TracingRef { get; set; }
    public int StockRef { get; set; }
    public decimal Quantity { get; set; }
    public decimal? SecondaryQuantity { get; set; }
    public decimal Fee { get; set; }
    public decimal Price { get; set; }
    public decimal PriceInBaseCurrency { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountInBaseCurrency { get; set; }
    public decimal PriceInfoDiscountRate { get; set; }
    public decimal PriceInfoPriceDiscount { get; set; }
    public decimal PriceInfoPercentDiscount { get; set; }
    public decimal CustomerDiscount { get; set; }
    public decimal CustomerDiscountRate { get; set; }
    public decimal Addition { get; set; }
    public decimal AdditionInBaseCurrency { get; set; }
    public decimal Tax { get; set; }
    public decimal TaxInBaseCurrency { get; set; }
    public decimal Duty { get; set; }
    public decimal DutyInBaseCurrency { get; set; }
    public decimal NetPriceInBaseCurrency { get; set; }
    public decimal Rate { get; set; }
    public int QuotationItemRef { get; set; }
    public string Description { get; set; }
    public string Description_En { get; set; }
    public int? DiscountInvoiceItemRef { get; set; }
    public int? ProductPackRef { get; set; }
    public decimal? ProductPackQuantity { get; set; }
    public decimal AggregateAmountDiscountRate { get; set; }
    public decimal AggregateAmountPriceDiscount { get; set; }
    public decimal AggregateAmountPercentDiscount { get; set; }
    public int? OrderItemRef { get; set; }
    public decimal AdditionFactor_VatEffective { get; set; }
    public decimal AdditionFactorInBaseCurrency_VatEffective { get; set; }
    public decimal AdditionFactor_VatIneffective { get; set; }
    public decimal AdditionFactorInBaseCurrency_VatIneffective { get; set; }
    public int? DiscountItemGroupRef { get; set; }
    public decimal BankFeeForCurrencySale { get; set; }
    public decimal BankFeeForCurrencySaleInBaseCurrency { get; set; }
    public bool IsAggregateDiscountInvoiceItem { get; set; }
    public decimal TaxPayerCurrencyPurchaseRate { get; set; }
}
