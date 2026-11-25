using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxationListModel(TaxInvoicePage Invoices, string Search, bool? Export, string State, string Error = null);
