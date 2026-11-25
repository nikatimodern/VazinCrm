using CRM.Core.Domain.Factors.Repositories;

namespace CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;

public sealed record SalesInvoiceDeletionRequest(SalesInvoiceDestinationKey Key, string Token);
