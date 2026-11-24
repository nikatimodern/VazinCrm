namespace CRM.Core.ApplicationServices.Supplies.Dtos;

public sealed record SupplyDestinationKey(Guid SourceId, Guid SoftwareId, Guid PurchaseMappingId,
    int? ReceiptId = null, Guid? ReceiptMappingId = null);
public sealed record SupplyDestinationEditRow(int RowNumber, int? ItemId, string? Description);
public sealed record SupplyDestinationEditRequest(SupplyDestinationKey Key, string Token, int? VendorId,
    string? Description, IReadOnlyList<SupplyDestinationEditRow> Items);
public sealed record SupplyReceiptEditRequest(SupplyDestinationKey Key, string Token, string DateText,
    int StockId, string? Description, IReadOnlyDictionary<int, string> RowDescriptions, bool AutoVoucher);
