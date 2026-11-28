using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Models;

namespace CRM.Core.Domain.Factors.Repositories;

public interface ISalesInvoiceDestinationRepository
{
    Task<SalesInvoiceDeletionPreview> PreviewAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task DeleteAsync(SalesInvoiceDestinationKey key, string token, string userId, string userName, CancellationToken ct);
    Task<bool> CompleteInterruptedDeletionAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task<SalesInvoiceEditState> GetEditAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task<bool> HasSyncDifferenceAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task<SalesInvoiceSyncPreview> GetSyncPreviewAsync(SalesInvoiceDestinationKey key, CancellationToken ct);
    Task SynchronizeAsync(SalesInvoiceSyncRequest request, int modifier,
        Func<CancellationToken, Task> lockPosting,
        Func<int, List<SepidarInvoiceExportItemPreview>, decimal, CancellationToken, Task<List<SalesInvoiceItemRecord>>> createRows,
        Func<SalesInvoicePosting, CancellationToken, Task> post, CancellationToken ct);
    Task EditAsync(SalesInvoiceDestinationKey key, string token, int modifier, SalesInvoiceEdit edit,
        Func<CancellationToken, Task> lockPosting, Func<SalesInvoicePosting, CancellationToken, Task> post, CancellationToken ct);
}

public sealed record SalesInvoiceDestinationKey(Guid SourceId, Guid ExternalSoftwareId, Guid MappingId);
public sealed record SalesInvoiceDeletionPreview(int Number, string SoftwareTitle, string Token,
    IReadOnlyList<int> VoucherNumbers, IReadOnlyList<SalesInvoiceDeliveryNumber> Deliveries, IReadOnlyList<string> Blockers);
public sealed record SalesInvoiceDeliveryNumber(int Number, int StockCode);
public sealed record SalesInvoiceEditRow(int Id, int Row, int ItemId, string? ItemTitle, int StockId,
    string? StockTitle, decimal Quantity, decimal Fee, bool LinkedToQuotation);
public sealed record SalesInvoiceEditState(SalesInvoiceDeletionPreview Impact, DateTime Date, int? QuotationId,
    int CustomerId, string? CustomerName, int SaleTypeId, int? BrokerPartyId, IReadOnlyList<SalesInvoiceEditRow> Rows,
    decimal Price, decimal Discount, decimal Rate, bool RequiresSynchronization);
public sealed record SalesInvoiceEditSelection(int Id, int ItemId, int StockId);
public sealed record SalesInvoiceEdit(DateTime Date, int Number, int? CustomerId, int? SaleTypeId,
    int? BrokerPartyId, IReadOnlyList<SalesInvoiceEditSelection> Items, bool AutoVoucher, bool AutoDelivery);
public sealed record SalesInvoicePosting(SalesInvoiceRecord Invoice, int? BrokerPartyId, IReadOnlyDictionary<int, int> PreviousDeliveryNumbers);
public sealed record SalesInvoiceSyncSelection(Guid SourceRowId, int ItemId, int StockId);
public sealed record SalesInvoiceSyncRequest(SalesInvoiceDestinationKey Key, string Token, SalesInvoiceSyncSelection[] Items,
    bool AutoVoucher, bool AutoDelivery);
public sealed record SalesInvoiceSyncAmounts(decimal Price, decimal Discount, decimal Tax, decimal Total);
public sealed record SalesInvoiceSyncRow(Guid Id, string ProductTitle, string UnitTxt, decimal Quantity, decimal Fee, decimal Price,
    decimal Discount, decimal Tax, int SuggestedItemId, int SuggestedStockId, int? QuotationItemId, string? ItemTitle, string? StockTitle);
public sealed record SalesInvoiceSyncDestinationRow(string? ItemTitle, string? StockTitle, decimal Quantity, decimal Fee,
    decimal Price, decimal Discount, decimal Tax);
public sealed record SalesInvoiceSyncPreview(SalesInvoiceDeletionPreview Impact, IReadOnlyList<SalesInvoiceSyncRow> SourceRows,
    IReadOnlyList<SalesInvoiceSyncDestinationRow> DestinationRows, SalesInvoiceSyncAmounts SourceAmounts,
    SalesInvoiceSyncAmounts DestinationAmounts, bool IsQuotationBased);
public sealed class SalesInvoiceDestinationException(string message) : InvalidOperationException(message);
