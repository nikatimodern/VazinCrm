using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Invoices.Entities;

namespace CRM.Core.Domain.Factors.Repositories;

public interface ISalesInvoiceDestinationRepository
{
    Task<SalesInvoiceDeletionPreview> PreviewAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task DeleteAsync(SalesInvoiceDestinationKey key, string token, string userId, string userName, CancellationToken ct);
    Task<bool> CompleteInterruptedDeletionAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task<SalesInvoiceEditState> GetEditAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task EditAsync(SalesInvoiceDestinationKey key, string token, int modifier, SalesInvoiceEdit edit,
        Func<CancellationToken, Task> lockPosting, Func<SalesInvoicePosting, CancellationToken, Task> post, CancellationToken ct);
}

public sealed record SalesInvoiceDestinationKey(Guid SourceId, Guid ExternalSoftwareId, Guid MappingId);
public sealed record SalesInvoiceDeletionPreview(int Number, string SoftwareTitle, string Token,
    IReadOnlyList<int> VoucherNumbers, IReadOnlyList<SalesInvoiceDeliveryNumber> Deliveries, IReadOnlyList<string> Blockers);
public sealed record SalesInvoiceDeliveryNumber(int Number, int StockCode);
public sealed record SalesInvoiceEditRow(int Id, int Row, int ItemId, string? ItemTitle, int StockId,
    string? StockTitle, decimal Quantity, bool LinkedToQuotation);
public sealed record SalesInvoiceEditState(SalesInvoiceDeletionPreview Impact, DateTime Date, int? QuotationId,
    int CustomerId, string? CustomerName, int SaleTypeId, int? BrokerPartyId, IReadOnlyList<SalesInvoiceEditRow> Rows);
public sealed record SalesInvoiceEditSelection(int Id, int ItemId, int StockId);
public sealed record SalesInvoiceEdit(DateTime Date, int Number, int? CustomerId, int? SaleTypeId,
    int? BrokerPartyId, IReadOnlyList<SalesInvoiceEditSelection> Items, bool AutoVoucher, bool AutoDelivery);
public sealed record SalesInvoicePosting(SalesInvoiceRecord Invoice, int? BrokerPartyId, IReadOnlyDictionary<int, int> PreviousDeliveryNumbers);
public sealed class SalesInvoiceDestinationException(string message) : InvalidOperationException(message);
