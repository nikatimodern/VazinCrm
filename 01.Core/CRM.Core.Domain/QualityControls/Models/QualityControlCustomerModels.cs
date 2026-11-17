#nullable enable
using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.QualityControls.Models;

public sealed class QualityControlCustomerListRequest
{
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public string? Search { get; set; }
    // Sort by actionable workload by default, before applying pagination.
    public string Sort { get; set; } = "pending";
    // null: all; 0: pending excluding contact-blocked customers; 1: fully reviewed;
    // 2: has discrepancy; 3: no answer; 4: invalid phone; 5: at least one reviewed item.
    public int? Status { get; set; } = 0;
}

public enum QualityControlCustomerContactStatus : byte
{
    None = 0,
    NoResponse = 1,
    InvalidNumber = 2
}

public sealed record QualityControlCustomerRow(
    Guid CustomerId, string? CustomerName, int CustomerCode, string? Mobile,
    int Orders, int Invoices, int TotalItems, int ReviewedItems, int DiscrepancyItems,
    QualityControlCustomerContactStatus ContactStatus, int NoResponseAttempts);

public sealed class SetQualityControlCustomerContactRequest
{
    public Guid CustomerId { get; set; }
    public QualityControlCustomerContactStatus Status { get; set; }
}

public sealed record SetQualityControlCustomerContactResult(
    int StatusCode, string Message, QualityControlCustomerContactStatus Status);

public sealed record QualityControlContactHistoryRow(
    Guid Id, QualityControlCustomerContactStatus Status, string CreatedAt,
    string? CreatedBy, int AttemptCount, bool IsLegacy, string? MobileAtAttempt,
    string? Description, bool IsDeleted, string? DeleteReason,
    string? DeletedBy, string? DeletedAt, string? RestoredBy, string? RestoredAt,
    byte[] RowVersion);

public sealed record QualityControlContactHistoryPage(
    Guid CustomerId, string? CustomerName, bool CanManage, int Total,
    IReadOnlyList<QualityControlContactHistoryRow> Data);

public sealed class ChangeQualityControlContactHistoryRequest
{
    public Guid CustomerId { get; set; }
    public Guid HistoryId { get; set; }
    public bool Restore { get; set; }
    public string? DeleteReason { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed record ChangeQualityControlContactHistoryResult(int StatusCode, string Message);

public sealed record QualityControlCustomerSummary(
    int InvalidNumberCustomers, int NoResponseCustomers,
    int PendingCustomers, int PendingItems,
    int ReviewedItems, int DiscrepancyItems, int NoDiscrepancyItems,
    int TotalItems, int CompletedCustomers, int DueFollowUpsToday, int TotalNoResponseAttempts);

public sealed record QualityControlCustomerListResult(
    int Total, int Filtered, IReadOnlyList<QualityControlCustomerRow> Data,
    QualityControlCustomerSummary Summary);

public sealed record QualityControlInvoiceItem(
    Guid FactorItemId, string? ProductSmallName, string? Product, string? WeightCount,
    string? UnitTxt, string? SupplierNames, string? SupplyExpert,
    QualityControlStatus Status, string? Description, byte[]? RowVersion,
    IReadOnlyList<QualityControlQuestionDto> Answers);

public sealed record QualityControlCustomerInvoice(Guid Id, int SerialNumber, string? Date,
    IReadOnlyList<QualityControlInvoiceItem> Items);

public sealed record QualityControlCustomerOrder(Guid Id, int SerialNumber, string? Title,
    IReadOnlyList<QualityControlCustomerInvoice> Invoices);

public sealed record QualityControlCustomerForm(Guid CustomerId, string? CustomerName, int CustomerCode,
    string? Mobile, string? AgentName, IReadOnlyList<QualityControlCustomerOrder> Orders,
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
