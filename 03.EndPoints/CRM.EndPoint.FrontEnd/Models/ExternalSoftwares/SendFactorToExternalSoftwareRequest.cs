using System;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public class SendFactorToExternalSoftwareRequest
{
    public Guid FactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    // Null means use the next invoice number in the selected Sepidar fiscal year.
    public int? InvoiceNumber { get; set; }
    // Optional per-invoice broker; the broker identity is always resolved from ExternalMappings.
    public bool IncludeBroker { get; set; }
    // The broker ID shown in Preview, used only to detect a mapping change before final submission.
    public int? PreviewBrokerPartyId { get; set; }
}
