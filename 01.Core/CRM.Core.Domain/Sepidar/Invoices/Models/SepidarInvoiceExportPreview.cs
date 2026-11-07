using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Sepidar.Invoices.Models;

public class SepidarInvoiceExportPreview
{
    public int QuotationId { get; set; }
    public int QuotationNumber { get; set; }
    public int InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; }
    public int SaleTypeRef { get; set; }
    public int FiscalYearRef { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal NetPrice { get; set; }
    public List<SepidarInvoiceExportItemPreview> Items { get; set; } = new();
}

public class SepidarInvoiceExportItemPreview
{
    public int RowId { get; set; }
    public int QuotationItemId { get; set; }
    public int ItemRef { get; set; }
    public string ItemTitle { get; set; }
    public int StockRef { get; set; }
    public string StockTitle { get; set; }
    public decimal Quantity { get; set; }
    public decimal Fee { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal NetPrice { get; set; }
}
