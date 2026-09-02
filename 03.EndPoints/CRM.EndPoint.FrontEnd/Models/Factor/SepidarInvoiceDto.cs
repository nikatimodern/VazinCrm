using System;

namespace CRM.EndPoint.FrontEnd.Models.Factor;

public class SepidarInvoiceDto {
    public int InvoiceId { get; init; }
    public int? Number { get; init; }
    public DateTime? Date { get; init; }
    public string DateTXT { get; init; }
    public int? CustomerPartyRef { get; init; }
    public string CustomerPartyName { get; init; }

    public int QuotationNumber { get; init; }
    public DateTime? CreationDate { get; init; }
}