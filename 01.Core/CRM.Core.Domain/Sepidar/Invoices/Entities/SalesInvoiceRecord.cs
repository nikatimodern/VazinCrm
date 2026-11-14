using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Invoices.Entities;

// Write model for SLS.Invoice. The existing Invoice entity remains mapped to SLS.vwInvoice.
[Table("Invoice", Schema = "SLS")]
public class SalesInvoiceRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int InvoiceId { get; set; }
    public int? QuotationRef { get; set; }
    public int CustomerPartyRef { get; set; }
    public string CustomerRealName { get; set; }
    public string CustomerRealName_En { get; set; }
    public int SaleTypeRef { get; set; }
    public int? PartyAddressRef { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public int CurrencyRef { get; set; }
    public int SLRef { get; set; }
    public int DeliveryLocationRef { get; set; }
    public int State { get; set; }
    public decimal Price { get; set; }
    public decimal PriceInBaseCurrency { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountInBaseCurrency { get; set; }
    public decimal Addition { get; set; }
    public decimal AdditionInBaseCurrency { get; set; }
    public decimal Tax { get; set; }
    public decimal TaxInBaseCurrency { get; set; }
    public decimal Duty { get; set; }
    public decimal DutyInBaseCurrency { get; set; }
    public decimal NetPriceInBaseCurrency { get; set; }
    public decimal Rate { get; set; }
    public int Version { get; set; }
    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int FiscalYearRef { get; set; }
    public int? VoucherRef { get; set; }
    public bool BaseOnInventoryDelivery { get; set; }
    public int? OrderRef { get; set; }
    public bool ShouldControlCustomerCredit { get; set; }
    public Guid? Guid { get; set; }
    public decimal AdditionFactor_VatEffective { get; set; }
    public decimal AdditionFactorInBaseCurrency_VatEffective { get; set; }
    public decimal AdditionFactor_VatIneffective { get; set; }
    public decimal AdditionFactorInBaseCurrency_VatIneffective { get; set; }
    public int? AgreementRef { get; set; }
    public DateTime? TaxPayerBillIssueDateTime { get; set; }
    public int? SettlementType { get; set; }
    public string Description { get; set; }
    public int? SignatureRef { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? NetPrice { get; private set; }

    public IList<SalesInvoiceItemRecord> InvoiceItems { get; set; } = new List<SalesInvoiceItemRecord>();
}
