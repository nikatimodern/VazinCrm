#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Querying;
using CRM.Core.Domain.Deals.Enums;

namespace CRM.Core.Domain.Deals.Repositories;
public sealed record OverdueSearch(DateTime Now, DateTime Cutoff, bool OwnOnly, string CreatorId, DateTime? StartDate, DateTime? EndDate, string? SortBy, string? SortDirection, bool WithFollowUp);
public sealed class OverdueDealRow
{
    public Guid Id { get; set; }
    public int SerialNumber { get; set; }
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CusCompanyName { get; set; }
    public int? CustomerCode { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? OverdueFollowUpAt { get; set; }
    public string? UserCreator { get; set; }
    public string? CommentText { get; set; }
}

public sealed class OverdueDealDetails
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string? UserCreatorID { get; set; }
    public DealFinancialStausEnum FinancialStatus { get; set; }
    public int SerialNumber { get; set; }
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CusCompanyName { get; set; }
    public int? CustomerCode { get; set; }
    public string? MobileNumber { get; set; }
    public DateTime CreateDate { get; set; }
    public string? UserCreator { get; set; }
    public string? CommentText { get; set; }
    public string? OverdueFollowUpNote { get; set; }
    public DateTime? OverdueFollowUpAt { get; set; }
    public DealStatusEnum Status { get; set; }
    public int ReportCount { get; set; }
    public int PreFactorCount { get; set; }
    public decimal PreFactorTotalAmount { get; set; }
    public int FactorCount { get; set; }
    public decimal FactorTotalAmount { get; set; }
    public int AccountancyCount { get; set; }
    public decimal AccountancyTotalAmount { get; set; }
}

public sealed record OverdueDealsResult(List<OverdueDealRow> Data, int OverdueCount);
public interface IOverdueDealRepository
{
    Task<OverdueDealsResult> GetListAsync(QueryFilter filter, OverdueSearch search, CancellationToken cancellationToken);
    Task<OverdueDealDetails?> GetDetailsAsync(Guid id, DateTime cutoff, bool ownOnly, string creatorId, CancellationToken cancellationToken);
}
