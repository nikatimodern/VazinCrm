#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Supplies.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.RequestAmounts.Repositories;
public sealed class RequestAmountRepository(CRMContextDb db) : IRequestAmountRepository, IAsyncDisposable
{
    private IDbContextTransaction? transaction;
    public Task<RequestAmount?> GetAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.RequestAmounts : db.RequestAmounts.AsNoTracking()).Include(r => r.RequestAmountOrderHeads).Include(r => r.RequestAmountSupplyFactor).Include(r => r.RequestAmountPayments).FirstOrDefaultAsync(r => r.Id == id, ct);
    public Task<List<RequestAmount>> GetByIdsAsync(List<Guid> ids, bool states, CancellationToken ct) => db.RequestAmounts.Include(r => r.RequestAmountPayments).Where(r => ids.Contains(r.Id) && (!states || r.RequestStatus == RequestStatusEnum.پرداخت_شده || r.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت)).ToListAsync(ct);
    public Task<List<RequestAmount>> GetByPaymentAsync(Guid id, CancellationToken ct) => db.RequestAmounts.AsNoTracking().Include(r => r.RequestAmountPayments.Where(p => p.PaymentId == id)).Where(r => r.RequestAmountPayments.Any(p => p.PaymentId == id)).ToListAsync(ct);
    public Task<Payment?> GetPaymentAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Payments : db.Payments.AsNoTracking()).Include(p => p.RequestAmountPayments).FirstOrDefaultAsync(p => p.Id == id, ct);
    public Task<Accountancy?> GetCashCreditAsync(Guid id, CancellationToken ct) => db.Accountancies.FirstOrDefaultAsync(a => a.RequestAmountId == id, ct);
    public Task<Guid?> GetCashierUserIdAsync(Guid cashierId, CancellationToken ct) =>
        db.Cashiers.AsNoTracking().Where(c => c.Id == cashierId).Select(c => (Guid?)c.UserId).FirstOrDefaultAsync(ct);

    public Task<string?> GetTargetPersonTextAsync(RequestAmountTypeEnum type, Guid targetPersonId, CancellationToken ct)
    {
        if (type == RequestAmountTypeEnum.تنخواه)
            return db.Cashiers.AsNoTracking().Where(c => c.Id == targetPersonId)
                .Select(c => c.UserTxt).FirstOrDefaultAsync(ct);

        var persons = db.Customers.AsNoTracking().Where(c => c.Id == targetPersonId);
        if (type == RequestAmountTypeEnum.عودت_به_مشتری)
            persons = persons.Where(c => c.RoleType == RoleTypeEnum.مشتری || (int)c.RoleType == 3);
        else
            persons = persons.Where(c => c.RoleType == RoleTypeEnum.تامین_کننده || (int)c.RoleType == 3);

        return persons.Select(c => ((c.CompanyName ?? "") + " " + (c.AgentName ?? "")).Trim()).FirstOrDefaultAsync(ct);
    }

    public Task<decimal> GetPaidAmountAsync(Guid id, CancellationToken ct) => db.RequestAmountPayments.Where(p => p.RequestAmountId == id).SumAsync(p => p.Amount, ct);
    public Task<Dictionary<Guid, RequestPaymentBalance>> GetPaymentBalancesAsync(List<Guid> ids, Guid excluded, CancellationToken ct) => db.RequestAmounts.AsNoTracking().Where(r => ids.Contains(r.Id)).Select(r => new RequestPaymentBalance(r.Id, r.AcceptedAmount, r.RequestAmountPayments.Where(p => p.PaymentId != excluded).Sum(p => p.Amount))).ToDictionaryAsync(r => r.Id, ct);
    public void Add(RequestAmount item) => db.RequestAmounts.Add(item);
    public void Apply(RequestAmount current, RequestAmount input)
    {
        db.Entry(current).CurrentValues.SetValues(input);
        current.RequestAmountSupplyFactor = input.RequestAmountSupplyFactor;
        current.RequestAmountOrderHeads = input.RequestAmountOrderHeads;
    }

    public void AddCredit(Accountancy item) => db.Accountancies.Add(item);
    public void RemoveCredit(Accountancy item) => db.Accountancies.Remove(item);
    public void SavePayment(Payment item, bool create)
    {
        if (create)
            db.Payments.Add(item);
        else
        {
            db.Payments.Update(item);
            db.Entry(item).Property(p => p.PaymentNumber).IsModified = false;
        }
    }

    public void SaveAttachments(IList<Attachment> attachments) => db.Attachments.UpdateRange(attachments);
    public async Task BeginAsync(CancellationToken ct)
    {
        transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task CommitAsync(CancellationToken ct)
    {
        if (transaction != null)
            await transaction.CommitAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (transaction != null)
            await transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
    }

    private IQueryable<SupplierFinancialSummaryVModel> GetSupplierFinancialSummaries()
    {
        var supplyAmounts = db.SupplyFactors.AsNoTracking().GroupBy(x => x.SupplierId).Select(g => new { SupplierId = g.Key, SupplyAmount = g.Sum(x => x.PaymentAmount), PaidAmount = 0m });
        var paidAmounts = db.RequestAmountPayments.AsNoTracking()
            .Where(x => x.RequestAmount.TargetPersonId.HasValue
                && x.RequestAmount.RequestAmountType != RequestAmountTypeEnum.تنخواه
                && x.RequestAmount.RequestAmountType != RequestAmountTypeEnum.عودت_به_مشتری)
            .GroupBy(x => x.RequestAmount.TargetPersonId!.Value)
            .Select(g => new { SupplierId = g.Key, SupplyAmount = 0m, PaidAmount = g.Sum(x => x.Amount) });
        return supplyAmounts.Concat(paidAmounts).GroupBy(x => x.SupplierId).Select(g => new SupplierFinancialSummaryVModel { SupplierId = g.Key, TotalSupplyAmount = g.Sum(x => x.SupplyAmount), TotalPaidAmount = g.Sum(x => x.PaidAmount), SupplierDebt = g.Sum(x => x.SupplyAmount) - g.Sum(x => x.PaidAmount) });
    }

    public async Task<RequestAmountList> GetListAsync(int type, string? search, int searchNumber, Guid? orderHeadId, string userId, bool readAll, CancellationToken ct)
    {
        var baseQuery = db.RequestAmounts.OrderByDescending(f => f.CreateDate).AsNoTracking();
        if (!readAll)
        {
            baseQuery = baseQuery.Where(f => f.UserCreatorID == userId);
        }

        if (orderHeadId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.RequestAmountOrderHeads.Any(ra => ra.OrderHeadId == orderHeadId.Value));
        }

        if (!string.IsNullOrEmpty(search))
        {
            baseQuery = baseQuery.Where(f => f.RequestNumber == searchNumber || f.Description.Contains(search) || f.TargetPersonTxt.Contains(search) || f.UserCreator.Contains(search) || f.RequestAmountSupplyFactor.Any(raSf => db.SupplyFactors.Any(sf => sf.Id == raSf.SupplyFactorId && sf.SerialNumber == search)) || f.RequestAmountOrderHeads.Any(raOh => db.OrderHeads.Any(oh => oh.Id == raOh.OrderHeadId && oh.OrderNumber.ToString() == search)));
        }

        var filterCounts = new Dictionary<int, int>
        {
            [-1] = await baseQuery.CountAsync(ct),
            [1] = await baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل || f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی).CountAsync(ct),
            [2] = await baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت).CountAsync(ct),
            [3] = await baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.پرداخت_شده).CountAsync(ct),
        };
        var query = type switch
        {
            1 => baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل || f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی),
            2 => baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت),
            3 => baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.پرداخت_شده),
            _ => baseQuery
        };
        var supplierFinancials = GetSupplierFinancialSummaries();
        var data = await query.GroupJoin(supplierFinancials,
            ra => ra.RequestAmountType == RequestAmountTypeEnum.تنخواه || ra.RequestAmountType == RequestAmountTypeEnum.عودت_به_مشتری ? null : ra.TargetPersonId,
            sf => (Guid?)sf.SupplierId, (ra, financials) => new { RequestAmount = ra, Financial = financials.FirstOrDefault() }).Select(x => new RequestAmountRow { RequestAmount = x.RequestAmount, SupplyFactorSerials = x.RequestAmount.RequestAmountSupplyFactor.Select(x => x.SupplyFactor.SerialNumber).ToList(), OrderHeadSerials = x.RequestAmount.RequestAmountOrderHeads.Select(x => x.OrderHead.OrderNumber).ToList(), CustomerNames = x.RequestAmount.RequestAmountOrderHeads.Select(x => x.OrderHead.CustomerTxt).ToList(), TotalPaymentAmount = x.RequestAmount.RequestAmountPayments.Sum(x => x.Amount), SupplierDebt = x.Financial != null ? x.Financial.SupplierDebt : 0, TotalSupplyFactor = x.Financial != null ? x.Financial.TotalSupplyAmount : 0, TotalSupplyPayment = x.Financial != null ? x.Financial.TotalPaidAmount : 0, }).ToListAsync(ct);
        return new(data, filterCounts);
    }

    public async Task<RequestAmountRow?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var data = await db.RequestAmounts.AsNoTracking().Where(f => f.Id == id).Include(f => f.RequestAmountOrderHeads).Include(f => f.RequestAmountSupplyFactor).Select(ra => new RequestAmountRow { RequestAmount = ra, SupplyFactorSerials = ra.RequestAmountSupplyFactor.Join(db.SupplyFactors, raSf => raSf.SupplyFactorId, sf => sf.Id, (raSf, sf) => sf.SerialNumber).ToList(), OrderHeadSerials = ra.RequestAmountOrderHeads.Join(db.OrderHeads, raOh => raOh.OrderHeadId, oh => oh.Id, (raOh, oh) => oh.OrderNumber).ToList(), }).FirstOrDefaultAsync(ct);
        return data;
    }

    public async Task<RequestPaymentsTable> GetPaymentsTableAsync(Guid id, TableRequest table, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var query = db.Payments.Where(p => p.RequestAmountPayments.Any(x => x.RequestAmountId == id)).Select(p => new RequestPaymentRow { Payment = p, RequestPaymentAmount = p.RequestAmountPayments.Where(x => x.RequestAmountId == id).Select(x => x.Amount).FirstOrDefault() }).OrderByDescending(f => f.Payment.CreateDate).AsNoTracking();
        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
        {
            var isAscending = sortColumnDirection == "asc";
            query = sortColumn switch
            {
                "PaymentNumber" => isAscending ? query.OrderBy(f => f.Payment.PaymentNumber) : query.OrderByDescending(f => f.Payment.PaymentNumber),
                "RequestPaymentAmount" => isAscending ? query.OrderBy(f => f.RequestPaymentAmount) : query.OrderByDescending(f => f.RequestPaymentAmount),
                "CreateDateTxt" => isAscending ? query.OrderBy(f => f.Payment.CreateDate) : query.OrderByDescending(f => f.Payment.CreateDate),
                "PaymentDateTxt" => isAscending ? query.OrderBy(f => f.Payment.PaymentDate) : query.OrderByDescending(f => f.Payment.PaymentDate),
                _ => query
            };
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        var data = await query.ToListAsync(ct);
        return new(draw, recordsTotal, recordsTotal, data);
    }

    public async Task<RequestAmountsTable> GetTableAsync(TableRequest table, int searchNumber, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var query = db.RequestAmounts.OrderByDescending(f => f.CreateDate).AsNoTracking();
        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(f => f.Amount == searchNumber);
        }

        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
        {
            var isAscending = sortColumnDirection == "asc";
            query = sortColumn switch
            {
                "Amount" => isAscending ? query.OrderBy(f => f.Amount) : query.OrderByDescending(f => f.Amount),
                "AcceptedAmount" => isAscending ? query.OrderBy(f => f.AcceptedAmount) : query.OrderByDescending(f => f.AcceptedAmount),
                "DocType" => isAscending ? query.OrderBy(f => f.DocType) : query.OrderByDescending(f => f.DocType),
                _ => query
            };
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        var data = await query.ToListAsync(ct);
        return new(draw, recordsTotal, recordsTotal, data);
    }
}
