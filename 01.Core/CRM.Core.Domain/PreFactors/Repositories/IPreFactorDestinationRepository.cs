using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.PreFactors.Repositories;

public interface IPreFactorDestinationRepository
{
    Task<PreFactorDestination?> GetAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task<SepidarPreFactorPrint?> GetPrintAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task<IReadOnlyList<PreFactorRelatedInvoice>> GetRelatedInvoicesAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task SaveAsync(PreFactorDestinationEdit edit, int externalUserId, CancellationToken ct);
    Task DeleteAsync(PreFactorDestinationDelete request, string userId, string userName, CancellationToken ct);
    Task<bool> CompleteInterruptedDeletionAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task<bool> HasSyncDifferenceAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task<PreFactorSyncPreview> GetSyncPreviewAsync(PreFactorDestinationKey key, CancellationToken ct);
    Task<bool> SynchronizeAsync(PreFactorSyncRequest request, int externalUserId, CancellationToken ct);
}

public record PreFactorDestinationKey(Guid SourceId, Guid ExternalSoftwareId, Guid MappingId);
public sealed record PreFactorDestinationDelete(PreFactorDestinationKey Key, int Version, DateTime LastModificationDate);
public sealed record PreFactorDestinationEdit(PreFactorDestinationKey Key, int Version, DateTime LastModificationDate,
    int SaleTypeId, int CustomerId, IReadOnlyList<PreFactorDestinationSelection> Items);
public sealed record PreFactorDestinationSelection(int Id, int ItemId, int StockId);
public sealed record PreFactorRelatedInvoice(int Id, int Number, DateTime Date, string CustomerName,
    DateTime CreationDate, string? RegisteredAt = null, string? RegisteredBy = null);
public sealed record PreFactorDestinationRow(int Id, int Row, int ItemId, string ItemTitle, int StockId, string StockTitle,
    decimal Quantity, decimal Fee, decimal UsedQuantity, decimal DeliveredQuantity, bool CanChangeItem, bool CanChangeStock);
public sealed record PreFactorDestination(int Number, string SoftwareTitle, int Version, DateTime LastModificationDate,
    int SaleTypeId, int CustomerId, string CustomerName, bool CanChangeCustomer, string? Restriction,
    IReadOnlyList<string> DeleteBlockers, IReadOnlyList<PreFactorDestinationRow> Items);

public sealed class PreFactorDestinationException(string message) : InvalidOperationException(message);

public sealed record PreFactorSyncRequest(PreFactorDestinationKey Key, string Token, IReadOnlyList<PreFactorSyncSelection> Items);
public sealed record PreFactorSyncSelection(Guid SourceRowId, int ItemId, int StockId);
public sealed record PreFactorSyncAmounts(decimal Price, decimal Discount, decimal Tax, decimal Total);
public sealed record PreFactorSyncSourceRow(Guid Id, string ProductTitle, string UnitTxt, decimal Quantity, decimal Fee, decimal Price, decimal Discount,
    decimal Tax, int? SuggestedItemId, int? SuggestedStockId);
public sealed record PreFactorSyncDestinationRow(int Row, string Title, string? ItemCode, string Stock, int? StockCode, decimal Quantity, decimal Fee, decimal Price,
    decimal Discount, decimal Tax);
public sealed record PreFactorSyncPreview(int Number, string SoftwareTitle, string Token, IReadOnlyList<string> Blockers,
    IReadOnlyList<PreFactorSyncSourceRow> SourceRows, IReadOnlyList<PreFactorSyncDestinationRow> DestinationRows,
    PreFactorSyncAmounts SourceAmounts, PreFactorSyncAmounts DestinationAmounts);
