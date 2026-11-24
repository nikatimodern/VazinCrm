using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Purchases.Entities;

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

public interface ISupplyDestinationMaintenanceRepository
{
    Task<SupplyPurchaseEditState> GetPurchaseAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task EditPurchaseAsync(SupplyDestinationDocument document, string token, int modifier,
        SupplyPurchaseEdit edit, CancellationToken ct);
    Task<SupplyDestinationDeletion> PreviewDeletionAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task DeleteAsync(SupplyDestinationDocument document, string token, CancellationToken ct);
    Task<SupplyReceiptEditState> GetReceiptEditAsync(SupplyDestinationDocument document, CancellationToken ct);
    Task EditReceiptAsync(SupplyDestinationDocument document, string token, int modifier, SupplyReceiptEdit edit,
        Func<SupplyReceiptPosting, CancellationToken, Task<int?>> postVoucher, CancellationToken ct);
}
