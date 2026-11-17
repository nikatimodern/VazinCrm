#nullable enable
using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.QualityControls.Models;

public sealed class QualityControlCustomerListRequest
{
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public string? Search { get; set; }
    // null: all; 0: has unchecked items; 1: fully checked; 2: has discrepancy.
    public int? Status { get; set; } = 0;
}

public sealed record QualityControlCustomerRow(
    Guid CustomerId, string? CustomerName, int CustomerCode, string? Mobile,
    int Orders, int Invoices, int TotalItems, int ReviewedItems, int DiscrepancyItems);

public sealed record QualityControlCustomerListResult(
    int Total, int Filtered, IReadOnlyList<QualityControlCustomerRow> Data);

public sealed record QualityControlInvoiceItem(
    Guid FactorItemId, string? ProductSmallName, string? Product, string? WeightCount,
    string? UnitTxt, string? SupplierNames, string? SupplyExpert,
    QualityControlStatus Status, string? Description, byte[]? RowVersion,
    IReadOnlyList<QualityControlQuestionDto> Answers);

public sealed record QualityControlCustomerInvoice(Guid Id, int SerialNumber, string? Date,
    IReadOnlyList<QualityControlInvoiceItem> Items);

public sealed record QualityControlCustomerOrder(Guid Id, int SerialNumber, string? Title,
    IReadOnlyList<QualityControlCustomerInvoice> Invoices)
{
    public bool HasLegacyReview { get; init; }
}

public sealed record QualityControlCustomerForm(Guid CustomerId, string? CustomerName, int CustomerCode,
    string? Mobile, IReadOnlyList<QualityControlCustomerOrder> Orders,
    IReadOnlyList<QualityControlQuestionDto> Questions);

public sealed class SaveQualityControlItemsRequest
{
    public Guid CustomerId { get; set; }
    public bool SaveAll { get; set; }
    public List<QualityControlItemInput>? Items { get; set; }
}

public sealed class QualityControlItemInput
{
    public Guid FactorItemId { get; set; }
    public QualityControlStatus Status { get; set; }
    public string? Description { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<QualityControlAnswerInput>? Answers { get; set; }
}

public sealed record QualityControlItemVersion(Guid FactorItemId, byte[] RowVersion);
public sealed record SaveQualityControlItemsResult(int StatusCode, string Message,
    IReadOnlyList<QualityControlItemVersion>? Versions = null);
