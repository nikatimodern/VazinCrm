using System;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class SendFactorToExternalSoftwareRequest
{
    public Guid FactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    // Null means use the next invoice number in the selected Sepidar fiscal year.
    public int? InvoiceNumber { get; set; }
    // Null explicitly means no broker for this invoice, even when the salesperson has a saved mapping.
    public int? BrokerPartyId { get; set; }
}
