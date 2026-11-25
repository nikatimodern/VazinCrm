using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Dtos;

public sealed record SupplyFinancialRow(Guid SupplyFactorItemId, int ItemId, string? Description);
public sealed record SupplyFinancialHistory(string? SerialNumber, IReadOnlyList<SupplyFinancialDestination> Softwares);
public sealed record SupplyFinancialDestination(Guid Id, string Title,
    SupplyFinancialDocumentHistory? Purchase, IReadOnlyList<SupplyFinancialDocumentHistory> Receipts)
{
    public SupplyFinancialDocumentHistory? Receipt => Receipts.FirstOrDefault();
}
public sealed record SupplyFinancialDocumentHistory(object? Document, string? DateText, Guid? MappingId,
    string? RegisteredAt, string? RegisteredBy);

public sealed record SupplyFinancialRequest
{
    public Guid SupplyFactorId { get; init; }
    public Guid ExternalSoftwareId { get; init; }
    public bool Receipt { get; init; }
    public int? PurchaseId { get; init; }
    public int VendorId { get; init; }
    public int StockId { get; init; }
    public DateTime Date { get; init; }
    public string? DateText { get; init; }
    public string? Search { get; init; }
    public string? Description { get; init; }
    public string? PreviewToken { get; init; }
    public IReadOnlyList<SupplyFinancialRow> Items { get; init; } = [];
    public IReadOnlyDictionary<int, string> RowDescriptions { get; init; } = new Dictionary<int, string>();
}

public interface ISupplyFinancialIntegration
{
    Task<SupplyResult> GetHistoryAsync(Guid sourceId, CancellationToken ct);
    Task<SupplyResult> PrepareAsync(SupplyFinancialRequest request, CancellationToken ct);
    Task<SupplyResult> SearchAsync(SupplyFinancialRequest request, bool items, CancellationToken ct);
    Task<SupplyResult> SaveVendorAsync(SupplyFinancialRequest request, CancellationToken ct);
    Task<SupplyResult> PreviewAsync(SupplyFinancialRequest request, CancellationToken ct);
    Task<SupplyResult> SubmitAsync(SupplyFinancialRequest request, CancellationToken ct);
    Task<SupplyResult> GetPurchaseEditAsync(SupplyDestinationKey key, CancellationToken ct);
    Task<SupplyResult> EditPurchaseAsync(SupplyDestinationEditRequest request, CancellationToken ct);
    Task<SupplyResult> GetReceiptEditAsync(SupplyDestinationKey key, CancellationToken ct);
    Task<SupplyResult> EditReceiptAsync(SupplyReceiptEditRequest request, CancellationToken ct);
    Task<SupplyResult> PreviewDestinationDeletionAsync(SupplyDestinationKey key, CancellationToken ct);
    Task<SupplyResult> DeleteDestinationAsync(SupplyDestinationKey key, string token, CancellationToken ct);
    Task<SupplyResult> DeleteMissingMappingAsync(Guid sourceId, Guid softwareId, Guid mappingId, bool receipt, CancellationToken ct);
}
