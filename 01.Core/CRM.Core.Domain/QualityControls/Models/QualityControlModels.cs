#nullable enable
using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.QualityControls.Models;

public enum QualityControlStatus
{
    NotReviewed = 0,
    NoDiscrepancy = 1,
    HasDiscrepancy = 2
}

public sealed record QualityControlAccess(string UserId, string UserName, bool ReadAll);

public sealed class QualityControlListRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public int? FinancialStatus { get; set; } = 1;
    public QualityControlStatus? Status { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public sealed class QualityControlRow
{
    public Guid DealId { get; set; }
    public int SerialNumber { get; set; }
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CreateDate { get; set; }
    public string? UserCreator { get; set; }
    public int? CustomerCode { get; set; }
    public bool FinancialApproved { get; set; }
    public QualityControlStatus Status { get; set; }
}

public sealed record QualityControlListResult(int Draw, int RecordsTotal, int RecordsFiltered, IReadOnlyList<QualityControlRow> Data);
public sealed record QualityControlQuestionDto(Guid Id, string Title, string QuestionText, string ZeroAnswerText, string OneAnswerText, int SortOrder, int? Answer);
public sealed record QualityControlDeal(Guid Id, int SerialNumber, string? Title, string? CustomerName, int? CustomerCode,
    Guid? CustomerId, string? UserCreator, string? CreateDate, string? Description, bool FinancialApproved,
    QualityControlDealCounts Counts)
{
    public string? CustomerTel { get; init; }
    public string? CustomerMobile { get; init; }
    public int CustomerPaymentCount { get; init; }
    public IReadOnlyList<QualityControlPaymentTotal> FactorTotals { get; init; } = Array.Empty<QualityControlPaymentTotal>();
    public IReadOnlyList<QualityControlPaymentTotal> CustomerPaymentTotals { get; init; } = Array.Empty<QualityControlPaymentTotal>();
}
public sealed record QualityControlPaymentTotal(string Currency, string Amount);
public sealed record QualityControlDealCounts(int PreFactor, int Factor, int Accountancy, int OrderHead, int Report, int SupplyFactor);
public sealed record QualityControlForm(QualityControlDeal Deal, string? Description, byte[]? RowVersion, IReadOnlyList<QualityControlQuestionDto> Questions);

public sealed class SaveQualityControlRequest
{
    public Guid DealId { get; set; }
    public string? Description { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<QualityControlAnswerInput>? Answers { get; set; }
}

public sealed record QualityControlAnswerInput(Guid QuestionId, int? Answer);
public sealed record QualityControlSaveResult(int StatusCode, string? Message = null, QualityControlStatus? Status = null, byte[]? RowVersion = null);
