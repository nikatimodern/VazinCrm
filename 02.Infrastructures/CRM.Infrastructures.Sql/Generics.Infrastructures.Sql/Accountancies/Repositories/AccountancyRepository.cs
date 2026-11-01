#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using CRM.Infrastructures.Sql.Querying;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Accountancies.Repositories;
public sealed class AccountancyRepository(CRMContextDb db) : IAccountancyRepository
{
    public Task<Accountancy?> GetAsync(Guid id, bool parent, bool tracking, CancellationToken ct) => (tracking ? db.Accountancies : db.Accountancies.AsNoTracking()).FirstOrDefaultAsync(a => parent ? a.ParentId == id : a.Id == id, ct);
    public Task<List<Attachment>> GetAttachmentsAsync(Guid id, CancellationToken ct) => db.Attachments.AsNoTracking().Where(a => a.EntityId == id).ToListAsync(ct);
    public Task<PreFactor?> GetPreFactorAsync(Guid? id, CancellationToken ct) => db.PreFactors.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    public Task<List<Accountancy>> GetWalletAsync(Guid customerId, CancellationToken ct) => db.Accountancies.AsNoTracking().ForCustomer(customerId).NotVosoled().Where(a => a.Wallet && a.PayType != PayTypeEnum.انتقال && a.AccType == AcEnum.واریزی).ToListAsync(ct);
    public Task<List<BankAccount>> GetBankAccountsAsync(BankAccountTypeEnum? type, CancellationToken ct) => db.BankAccounts.AsNoTracking().Where(b => !type.HasValue || b.BankAccountType == type).OrderBy(b => b.AccountName).ToListAsync(ct);
    public Task<BankAccount?> GetBankAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.BankAccounts : db.BankAccounts.AsNoTracking()).FirstOrDefaultAsync(b => b.Id == id, ct);
    public Task<List<Accountancy>> GetPendingAsync(Guid pfId, CancellationToken ct) => db.Accountancies.ForPreFactor(pfId).Where(a => a.Status == AccStatusEnum.ثبت_اولیه || a.Status == AccStatusEnum.رد_شده).ToListAsync(ct);
    public void SaveBank(BankAccount item, bool create)
    {
        if (create)
            db.BankAccounts.Add(item);
        else
            db.BankAccounts.Update(item);
    }

    public async Task RemoveTasksAsync(Guid id, CancellationToken ct)
    {
        var tasks = await db.Carizos.Where(t => t.EntityId == id).ToListAsync(ct);
        db.Carizos.RemoveRange(tasks);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task<InvoiceTotals?> GetTotalsAsync(int type, Guid? id, CancellationToken ct)
    {
        var preFactors = db.PreFactors.AsNoTracking();
        var acc = db.Accountancies.AsNoTracking();
        decimal discount, total, tax;
        if (type is 1 or 11 or 12)
        {
            var pf = await GetPreFactorAsync(id, ct);
            if (pf == null)
                return null;
            discount = pf.Discount;
            total = pf.SumTotalAmount;
            tax = pf.TotalTax;
            acc = acc.ForPreFactor(pf.Id).NotVosoled();
        }
        else if (type == 2)
        {
            var factor = await db.Factors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
            if (factor == null)
                return null;
            discount = factor.Discount;
            total = factor.SumTotalAmount;
            tax = factor.TotalTax;
            acc = acc.Where(a => a.fid == id && a.InvoiceType == InvoiceTypeEnum.فاکتور && a.AccType == AcEnum.واریزی);
        }
        else if (type is 3 or 4 or 5)
        {
            preFactors = type == 3 ? preFactors.Where(p => p.DealId == id) : preFactors.Where(p => p.CustomerId == id);
            var sums = await preFactors.GroupBy(p => 1).Select(g => new { Discount = g.Sum(p => p.Discount), Total = g.Sum(p => p.SumTotalAmount), Tax = g.Sum(p => p.TotalTax) }).FirstOrDefaultAsync(ct);
            discount = sums?.Discount ?? 0;
            total = sums?.Total ?? 0;
            tax = sums?.Tax ?? 0;
            acc = (type == 3 ? acc.Where(a => a.DealId == id) : acc.Where(a => a.CustomerId == id)).Where(a => a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && ((a.PayType != PayTypeEnum.اعتباری && a.PayType != PayTypeEnum.چکی) || ((a.PayType == PayTypeEnum.اعتباری || a.PayType == PayTypeEnum.چکی) && !a.Vosoled)));
        }
        else
            return null;
        var received = await acc.Where(a => a.AccType == AcEnum.واریزی).SumAsync(a => a.credit, ct) ?? 0;
        var returned = type == 2 ? 0 : await acc.Where(a => a.AccType == AcEnum.برگشتی).SumAsync(a => a.BackWard, ct) ?? 0;
        return new(discount, total, tax, received, returned);
    }

    public async Task<AccountancyPayments> GetPaymentsAsync(int type, Guid id, CancellationToken ct)
    {
        var query = db.Accountancies.AsNoTracking().Include(a => a.Deal).AsQueryable();
        PreFactor? pf = null;
        Deal? deal = null;
        switch (type)
        {
            case 1:
                query = query.ForPreFactor(id);
                pf = await GetPreFactorAsync(id, ct);
                break;
            case 2:
                query = query.ForFactor(id);
                break;
            case 3:
                query = query.ForDeal(id);
                deal = await db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
                break;
            case 4:
                query = query.ForCustomer(id).Where(a => a.Wallet);
                break;
            case 5:
                query = query.ForCustomer(id);
                break;
        }

        return new(await query.ToListAsync(ct), pf, deal);
    }

    public async Task<AccountancyCartable> GetCartableAsync(string? serVal, int searchNumber, CartableEnum cartable, int type, PayTypeEnum? payType, CancellationToken ct)
    {
        var baseQuery = db.Accountancies.AsNoTracking().Include(x => x.Deal).ForPreFactor().OrderByDescending(x => x.CreateDate).AsQueryable();
        if (cartable == CartableEnum.کارشناس_تامین)
        {
            baseQuery = baseQuery.OnlyTamin();
        }

        if (payType.HasValue)
        {
            baseQuery = baseQuery.Where(f => f.PayType == payType.Value);
        }

        if (!string.IsNullOrWhiteSpace(serVal))
        {
            var serNum = searchNumber;
            baseQuery = baseQuery.Where(f => f.pfSerialNumber == serNum || f.SerialNum == serNum || f.UserCreator.Contains(serVal) || f.CustomerName.Contains(serVal));
        }

        var filterCounts = new Dictionary<int, int>
        {
            [-1] = await baseQuery.CountAsync(ct),
            [1] = await baseQuery.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه).CountAsync(ct),
            [2] = await baseQuery.Where(f => f.Status == AccStatusEnum.تایید_شده).CountAsync(ct),
            [3] = await baseQuery.Where(f => f.Status == AccStatusEnum.رد_شده).CountAsync(ct),
        };
        var query = type switch
        {
            1 => baseQuery.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه),
            2 => baseQuery.Where(f => f.Status == AccStatusEnum.تایید_شده),
            3 => baseQuery.Where(f => f.Status == AccStatusEnum.رد_شده),
            _ => baseQuery
        };
        if (string.IsNullOrWhiteSpace(serVal))
        {
            query = query.Take(100);
        }

        var accS = await query.ToListAsync(ct);
        return new(accS, filterCounts);
    }

    public async Task<AccountancyTable> GetTableAsync(TableRequest table, QueryFilter filter, int accType, int payType, string userId, bool readAll, CancellationToken ct)
    {
        var draw = table.Draw;
        var skip = table.Start;
        var pageSize = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var recordsTotal = 0;
        var dataF = db.Accountancies.AsNoTracking().Where(f => f.FirstAcc == false && f.InvoiceType != InvoiceTypeEnum.تنخواه).AsQueryable();
        if (!readAll)
        {
            dataF = dataF.Where(f => f.UserCreatorID == userId);
        }

        if (filter.HasFilter)
        {
            dataF = dataF.ApplyFilter(filter, f => f.CustomerId, f => f.VosolDateM, f => f.UserCreatorID);
        }

        dataF = accType switch
        {
            1 => dataF.Where(f => f.AccType == AcEnum.واریزی).AsQueryable(),
            2 => dataF.Where(f => f.AccType == AcEnum.بدهکاری).AsQueryable(),
            3 => dataF.Where(f => f.AccType == AcEnum.برگشتی).AsQueryable(),
            _ => dataF
        };
        dataF = payType switch
        {
            1 => dataF.Where(f => f.PayType == PayTypeEnum.نقدی).AsQueryable(),
            2 => dataF.Where(f => f.PayType == PayTypeEnum.چکی).AsQueryable(),
            3 => dataF.Where(f => f.PayType == PayTypeEnum.اعتباری).AsQueryable(),
            4 => dataF.Where(f => f.PayType == PayTypeEnum.تهاتری).AsQueryable(),
            _ => dataF
        };
        if (!string.IsNullOrEmpty(searchValue))
        {
            dataF = dataF.Where(m => m.SerialNum.ToString() == searchValue || m.CustomerName.Contains(searchValue) || m.CustomerCode.ToString() == searchValue || m.BankAccountTxt.Contains(searchValue) || m.Beneficiary.Contains(searchValue) || m.ToName.Contains(searchValue) || m.credit.ToString() == searchValue || m.pfSerialNumber.ToString() == searchValue).AsQueryable();
        }

        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {
            if (sortColumnDirection == "asc")
            {
                dataF = sortColumn switch
                {
                    "UserCreator" => dataF.OrderBy(f => f.UserCreator),
                    "PfSerialNumber" => dataF.OrderBy(f => f.pfSerialNumber),
                    "BankAccountTxt" => dataF.OrderBy(f => string.IsNullOrEmpty(f.BankAccountTxt)).ThenBy(f => f.BankAccountTxt),
                    "Customer" => dataF.OrderBy(f => f.CustomerCode),
                    "VosolDate" => dataF.OrderBy(f => f.VosolDateM == null).ThenBy(f => f.VosolDateM),
                    "credit" => dataF.OrderBy(f => f.credit == null).ThenBy(f => f.credit),
                    _ => dataF.OrderBy(f => f.SerialNum)};
            }
            else
            {
                dataF = sortColumn switch
                {
                    "UserCreator" => dataF.OrderByDescending(f => f.UserCreator),
                    "PfSerialNumber" => dataF.OrderByDescending(f => f.pfSerialNumber),
                    "BankAccountTxt" => dataF.OrderBy(f => string.IsNullOrEmpty(f.BankAccountTxt)).ThenByDescending(f => f.BankAccountTxt),
                    "Customer" => dataF.OrderByDescending(f => f.CustomerCode),
                    "VosolDate" => dataF.OrderBy(f => f.VosolDateM == null).ThenByDescending(f => f.VosolDateM),
                    "credit" => dataF.OrderBy(f => f.credit == null).ThenByDescending(f => f.credit),
                    _ => dataF.OrderByDescending(f => f.SerialNum)};
            }
        }

        recordsTotal = await dataF.CountAsync(ct);
        List<Accountancy> data;
        if (pageSize == -1)
            data = await dataF.Skip(skip).ToListAsync(ct);
        else
            data = await dataF.Skip(skip).Take(pageSize).ToListAsync(ct);
        return new(draw, recordsTotal, recordsTotal, data);
    }

    public async Task<BankAccountTable> GetBankTableAsync(TableRequest table, int? type, CancellationToken ct)
    {
        var draw = table.Draw;
        var skip = table.Start;
        var pageSize = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var recordsTotal = 0;
        var dataF = db.BankAccounts.AsQueryable();
        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {
            if (sortColumnDirection == "asc")
            {
                dataF = sortColumn switch
                {
                    "AccountName" => dataF.OrderBy(f => f.AccountName),
                    _ => dataF.OrderBy(f => f.CreateDate)};
            }
            else
            {
                dataF = sortColumn switch
                {
                    "AccountName" => dataF.OrderByDescending(f => f.AccountName),
                    _ => dataF.OrderByDescending(f => f.CreateDate)};
            }
        }

        if (!string.IsNullOrEmpty(searchValue))
        {
            dataF = dataF.Where(m => m.AccountName.Contains(searchValue));
        }

        if (type.HasValue && type != -1)
        {
            dataF = dataF.Where(f => f.BankAccountType == (BankAccountTypeEnum)type);
        }

        if (pageSize != -1)
            dataF = dataF.Skip(skip).Take(pageSize);
        var data = await dataF.ToListAsync(ct);
        recordsTotal = await dataF.CountAsync(ct);
        return new(draw, recordsTotal, recordsTotal, data);
    }
}
