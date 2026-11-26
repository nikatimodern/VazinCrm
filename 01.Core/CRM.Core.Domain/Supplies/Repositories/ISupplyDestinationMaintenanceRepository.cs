using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.Core.Domain.Supplies.Repositories;

public sealed record SupplyDestinationDocument(Guid SoftwareId, int PurchaseId, int? ReceiptId);
public sealed record SupplyPurchaseEditRow(int RowNumber, int ItemId, decimal Quantity, decimal Fee, string? Description);
public sealed record SupplyPurchaseEditState(int Number, DateTime Date, int VendorId, string? VendorName,
    string? Description, IReadOnlyList<SupplyPurchaseEditRow> Rows, string Token, string? Restriction, bool HasLinkedReceipt);
public sealed record SupplyPurchaseEdit(int? VendorId, string? Description, IReadOnlyList<SupplyPurchaseEditRow> Rows);
public sealed record SupplyDestinationDeletion(int Number, int? VoucherNumber, string Token, IReadOnlyList<string> Blockers);
public sealed record SupplyReceiptEditState(SupplyDestinationDeletion Impact, DateTime Date, int StockId, string? StockTitle,
    string? Description, IReadOnlyList<SupplyReceiptEditRow> Rows);
public sealed record SupplyReceiptEditRow(int Id, int ItemId, decimal Quantity, string? Description);
public sealed record SupplyReceiptEdit(DateTime Date, int StockId, string? Description, IReadOnlyDictionary<int, string> RowDescriptions);
public sealed record SupplyReceiptPosting(InventoryReceipt Receipt, int? SupplierInvoiceNumber, bool StockChanged);

public sealed record SupplySyncAmounts(decimal Quantity, decimal Fee, decimal Price, decimal Discount, decimal Tax, decimal Total);
public sealed record SupplySyncSourceRow(Guid Id, string Product, string Unit, SupplySyncAmounts Amounts,
    int? SuggestedItemId, string? Description);
public sealed record SupplySyncDestinationRow(int ItemId, string ItemLabel, string? Description, SupplySyncAmounts Amounts);
public sealed record SupplySyncStatus(bool PurchaseChanged, bool ReceiptChanged);
public sealed record SupplySyncPreview(string Token, int PurchaseNumber, int? ReceiptNumber, string? StockTitle,
    int? VoucherNumber, int VendorId, string? Description, IReadOnlyList<string> Blockers,
    IReadOnlyList<SupplySyncSourceRow> SourceRows, IReadOnlyList<SupplySyncDestinationRow> PurchaseRows,
    IReadOnlyList<SupplySyncDestinationRow> ReceiptRows);

public interface ISupplyDestinationMaintenanceRepository
{
    Task<SupplySyncStatus> GetSyncStatusAsync(SupplyDestinationDocument document, SupplyFactor source, CancellationToken ct);
    Task<SupplySyncPreview> GetSyncPreviewAsync(SupplyDestinationDocument document, SupplyFactor source, bool receiptOnly, CancellationToken ct);
    Task SynchronizeAsync(SupplyDestinationDocument document, SupplyFactor source, bool receiptOnly, string token,
        bool autoVoucher, int modifier,
        Func<SupplySyncPreview, CancellationToken, Task<IReadOnlyList<InventoryPurchaseInvoiceItem>>> prepareRows,
        Func<bool, int, CancellationToken, Task<int>> allocateRowIds,
        Func<SupplyReceiptPosting, CancellationToken, Task<int?>> postVoucher, CancellationToken ct);
    Task<SupplyPurchaseEditState> GetPurchaseAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task EditPurchaseAsync(SupplyDestinationDocument document, string token, int modifier,
        SupplyPurchaseEdit edit, CancellationToken ct);
    Task<SupplyDestinationDeletion> PreviewDeletionAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task DeleteAsync(SupplyDestinationDocument document, string token, CancellationToken ct);
    Task<SupplyReceiptEditState> GetReceiptEditAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task EditReceiptAsync(SupplyDestinationDocument document, string token, int modifier, SupplyReceiptEdit edit,
        Func<SupplyReceiptPosting, CancellationToken, Task<int?>> postVoucher, CancellationToken ct);
}
