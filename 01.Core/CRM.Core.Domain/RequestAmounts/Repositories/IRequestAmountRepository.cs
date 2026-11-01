#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Querying;

namespace CRM.Core.Domain.RequestAmounts.Repositories;
public class RequestAmountRow
{
    public CRM.Core.Domain.RequestAmounts.Entities.RequestAmount RequestAmount { get; set; }
    public List<string> SupplyFactorSerials { get; set; }
    public List<int> OrderHeadSerials { get; set; }
    public List<string> CustomerNames { get; set; }
    public decimal TotalPaymentAmount { get; set; }
    public decimal SupplierDebt { get; set; }
    public decimal TotalSupplyFactor { get; set; }
    public decimal TotalSupplyPayment { get; set; }
}

public sealed record RequestAmountList(List<RequestAmountRow> Data, Dictionary<int, int> FilterCounts);
public sealed class RequestPaymentRow
{
    public Payment Payment { get; set; } = null !;
    public decimal RequestPaymentAmount { get; set; }
}

public sealed record RequestPaymentsTable(string? Draw, int RecordsTotal, int RecordsFiltered, List<RequestPaymentRow> Data);
public sealed record RequestAmountsTable(string? Draw, int RecordsTotal, int RecordsFiltered, List<RequestAmount> Data);
public sealed record RequestPaymentBalance(Guid Id, decimal AcceptedAmount, decimal PaidAmount);
public interface IRequestAmountRepository
{
    Task<RequestAmount?> GetAsync(Guid id, bool tracking, CancellationToken ct);
    Task<List<RequestAmount>> GetByIdsAsync(List<Guid> ids, bool paymentStatesOnly, CancellationToken ct);
    Task<List<RequestAmount>> GetByPaymentAsync(Guid id, CancellationToken ct);
    Task<Payment?> GetPaymentAsync(Guid id, bool tracking, CancellationToken ct);
    Task<Accountancy?> GetCashCreditAsync(Guid id, CancellationToken ct);
    Task<decimal> GetPaidAmountAsync(Guid id, CancellationToken ct);
    Task<Dictionary<Guid, RequestPaymentBalance>> GetPaymentBalancesAsync(List<Guid> ids, Guid excludedPayment, CancellationToken ct);
    Task<RequestAmountList> GetListAsync(int type, string? search, int searchNumber, Guid? orderHeadId, string userId, bool readAll, CancellationToken ct);
    Task<RequestAmountRow?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<RequestPaymentsTable> GetPaymentsTableAsync(Guid id, TableRequest table, CancellationToken ct);
    Task<RequestAmountsTable> GetTableAsync(TableRequest table, int searchNumber, CancellationToken ct);
    void Add(RequestAmount item);
    void Apply(RequestAmount current, RequestAmount input);
    void AddCredit(Accountancy item);
    void RemoveCredit(Accountancy item);
    void SavePayment(Payment item, bool create);
    void SaveAttachments(IList<Attachment> attachments);
    Task BeginAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
