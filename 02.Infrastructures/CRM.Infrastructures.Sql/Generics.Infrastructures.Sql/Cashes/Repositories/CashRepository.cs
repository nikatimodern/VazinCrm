#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Infrastructures.Sql.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.Cashes.Repositories;
public sealed class CashRepository(CRMContextDb db) : ICashRepository, IAsyncDisposable
{
    private IDbContextTransaction? transaction;
    public Task<Cash?> GetAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Cashes : db.Cashes.AsNoTracking()).Include(c => c.CashOrderHeads).Include(c => c.CostCenter).Include(c => c.CostTitle).Include(c => c.CashExternalSoftwares).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<CashDetail?> GetDetailAsync(Guid id, CancellationToken ct) => db.Cashes.AsNoTracking().Where(c => c.Id == id).Select(c => new CashDetail(c, c.CashOrderHeads.Select(o => o.OrderHead.OrderNumber).ToList(), db.Attachments.Where(a => a.EntityId == c.Id && a._AttachType == _AttachType.Cash).ToList())).FirstOrDefaultAsync(ct);
    public Task<Cashier?> GetCashierAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Cashiers : db.Cashiers.AsNoTracking()).Include(c => c.CashierExternalSoftwares).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<BaseCash?> GetBaseAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.BaseCashes : db.BaseCashes.AsNoTracking()).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<CashGroup?> GetGroupAsync(Guid id, CancellationToken ct) => db.CashGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
    public Task<List<Cash>> GetGroupItemsAsync(Guid id, bool export, CancellationToken ct)
    {
        var query = db.Cashes.Where(c => c.CashGroupId == id);
        if (export)
            query = query.Include(c => c.CashGroup).Include(c => c.CostCenter).Include(c => c.CostTitle).Include(c => c.BankAccount).OrderBy(c => c.CreateDate);
        return query.ToListAsync(ct);
    }

    public Task<List<Cash>> GetDraftsAsync(string userId, CancellationToken ct) => db.Cashes.Where(c => c.UserCreatorID == userId && c.Status == CashStatusEnum.ثبت_موقت).ToListAsync(ct);
    public Task<List<Cash>> GetByIdsAsync(List<Guid> ids, CancellationToken ct) => db.Cashes.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
    public Task<Accountancy?> GetDebitAsync(Guid id, CancellationToken ct) => db.Accountancies.FirstOrDefaultAsync(a => a.CashId == id, ct);
    public Task<List<CashOption<Guid>>> GetCentersAsync(CancellationToken ct) => db.BaseCenterCashes.AsNoTracking().Select(c => new CashOption<Guid>(c.Id, c.Title)).ToListAsync(ct);
    public Task<List<CashOption<Guid>>> GetTitlesAsync(bool supply, CancellationToken ct) => db.BaseCashes.AsNoTracking().Where(c => supply ? c.BaseCashType == BaseCashTypeEnum.تامین || c.BaseCashType == BaseCashTypeEnum.عمومی : c.BaseCashType == BaseCashTypeEnum.عمومی).Select(c => new CashOption<Guid>(c.Id, c.CostTitle)).ToListAsync(ct);
    public Task<List<CashOption<Guid>>> GetOwnCashiersAsync(string userId, CancellationToken ct)
    {
        var id = Guid.Parse(userId);
        return db.Cashiers.AsNoTracking().Where(c => c.UserId == id).Select(c => new CashOption<Guid>(c.Id, c.Title)).ToListAsync(ct);
    }

    public Task<List<CashierOption>> GetCashierOptionsAsync(string userId, CancellationToken ct)
    {
        var id = Guid.Parse(userId);
        return db.Cashiers.AsNoTracking().Select(c => new CashierOption(c.UserId, c.UserTxt, c.UserId == id)).ToListAsync(ct);
    }

    public async Task<CashierBalanceDto> GetBalanceAsync(Guid id, CancellationToken ct)
    {
        var data = await db.Accountancies.AsNoTracking().Where(a => a.CashierId == id).Select(a => new { a.credit, a.debit, a.Accepted }).ToListAsync(ct);
        return new()
        {
            CashierId = id,
            TotalCredit = data.Sum(a => a.credit),
            TotalDebit = data.Sum(a => a.debit),
            isAccepted = data.All(a => a.Accepted == true)
        };
    }

    public Task<List<int>> GetUsedVoucherNumbersAsync(int start, int count, CancellationToken ct)
    {
        var end = start + count - 1;
        return db.Cashes.Where(c => c.PaymentVoucherNumber != null && c.PaymentVoucherNumber >= start && c.PaymentVoucherNumber <= end).Select(c => c.PaymentVoucherNumber!.Value).OrderBy(c => c).ToListAsync(ct);
    }

    public Task<bool> IsDuplicateBaseAsync(BaseCash item, CancellationToken ct) => db.BaseCashes.AnyAsync(c => c.Id != item.Id && (c.MoeinCode == item.MoeinCode || c.CostTitle.Trim() == item.CostTitle.Trim()), ct);
    public void Add(Cash item) => db.Cashes.Add(item);
    public void Apply(Cash current, Cash input)
    {
        db.Entry(current).CurrentValues.SetValues(input);
        db.Entry(current).Property(c => c.Serial).IsModified = false;
        current.CashOrderHeads = input.CashOrderHeads;
    }

    public void AddCashier(Cashier item) => db.Cashiers.Add(item);
    public void RemoveCashierMappings(Cashier item) => db.CashierExternalSoftwares.RemoveRange(item.CashierExternalSoftwares);
    public void SaveBase(BaseCash item, bool create)
    {
        if (create)
            db.BaseCashes.Add(item);
        else
            db.BaseCashes.Update(item);
    }

    public void AddGroup(CashGroup item) => db.CashGroups.Add(item);
    public void RemoveGroup(CashGroup item) => db.CashGroups.Remove(item);
    public void AddDebit(Accountancy item) => db.Accountancies.Add(item);
    public void RemoveDebit(Accountancy item) => db.Accountancies.Remove(item);
    public void SaveAttachments(IList<Attachment> items) => db.Attachments.UpdateRange(items);
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

    public async Task<CashListTable> GetTableAsync(TableRequest table, QueryFilter filter, string userId, bool readAll, int searchNumber, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var isExported = false;
        var query = db.Cashes.AsNoTracking().AsQueryable();
        var mappedCash = await db.ExternalMappings.AsNoTracking().Where(f => f.MapType == ExternalMappingTypeEnum.تنخواه && f.EntityName == nameof(Cash)).Select(f => new { f.EntityId, SoftwareTitle = f.ExternalSoftware.Title }).ToListAsync(ct);
        if (Guid.TryParse(filter.EntityId, out var groupId))
        {
            query = query.Where(f => f.CashGroupId == groupId);
        }

        if (!readAll)
        {
            query = query.Where(f => f.UserCreatorID == userId);
        }
        else
        {
            query = query.Where(f => f.Status != CashStatusEnum.ثبت_موقت || f.UserCreatorID == userId);
        }

        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(f => f.Amount == searchNumber || f.Serial == searchNumber || f.CostDescription.Contains(searchValue) || f.CostCenterTxt.Contains(searchValue) || f.CostTitleTxt.Contains(searchValue));
        }

        if (filter.HasFilter)
        {
            query = query.ApplyFilter(filter, null, d => d.CreateDate, f => f.UserCreatorID);
        }

        query = filter.Type switch
        {
            1 => query.Where(f => f.Status == CashStatusEnum.در_دست_تایید),
            2 => query.Where(f => f.Status == CashStatusEnum.تایید_شده),
            _ => query
        };
        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
        {
            var isAscending = sortColumnDirection == "asc";
            query = sortColumn switch
            {
                "serial" => isAscending ? query.OrderBy(f => f.Serial) : query.OrderByDescending(f => f.Serial),
                "createDateTxt" => isAscending ? query.OrderBy(f => f.CreateDate) : query.OrderByDescending(f => f.CreateDate),
                "costTitleTxt" => isAscending ? query.OrderBy(f => f.CostTitleTxt) : query.OrderByDescending(f => f.CostTitleTxt),
                "costCenterTxt" => isAscending ? query.OrderBy(f => f.CostCenterTxt) : query.OrderByDescending(f => f.CostCenterTxt),
                "costDate" => isAscending ? query.OrderBy(f => f.CostDateMiladi) : query.OrderByDescending(f => f.CostDateMiladi),
                "amount" => isAscending ? query.OrderBy(f => f.Amount) : query.OrderByDescending(f => f.Amount),
                "fee" => isAscending ? query.OrderBy(f => f.Fee) : query.OrderByDescending(f => f.Fee),
                "totalAmount" => isAscending ? query.OrderBy(f => f.Amount + f.Fee) : query.OrderByDescending(f => f.Amount + f.Fee),
                "costDescription" => isAscending ? query.OrderBy(f => f.CostDescription) : query.OrderByDescending(f => f.CostDescription),
                "userCreator" => isAscending ? query.OrderBy(f => f.UserCreator) : query.OrderByDescending(f => f.UserCreator),
                "paymentVoucherNumber" => isAscending ? query.OrderBy(f => f.CashGroup == null).ThenBy(f => f.PaymentVoucherNumber) : query.OrderBy(f => f.CashGroup == null).ThenByDescending(f => f.PaymentVoucherNumber),
                _ => query
            };
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        object cashierBalance = null;
        var data = await query.ToListAsync(ct);
        var softwareTitles = mappedCash.GroupBy(f => f.EntityId).ToDictionary(g => g.Key, g => g.Select(f => f.SoftwareTitle).Distinct().ToList());
        foreach (var cash in data)
        {
            cash.ExternalSoftwareTitles = softwareTitles.GetValueOrDefault(cash.Id);
        }

        await AppendOrderHeadNumbersToCashDescriptions(data, ct);
        return new(draw, recordsTotal, recordsTotal, data, isExported, cashierBalance);
    }

    private async Task AppendOrderHeadNumbersToCashDescriptions(List<Cash> cashes, CancellationToken ct)
    {
        var cashIds = cashes.Select(x => x.Id).ToList();
        var orderNumbers = await db.CashOrderHeads.AsNoTracking().Where(x => cashIds.Contains(x.CashId)).Select(x => new { x.CashId, x.OrderHead.OrderNumber }).ToListAsync(ct);
        var orderNumbersLookup = orderNumbers.ToLookup(x => x.CashId, x => x.OrderNumber);
        foreach (var cash in cashes)
        {
            var numbers = orderNumbersLookup[cash.Id].ToList();
            if (numbers.Count > 0)
            {
                cash.CostDescription += Environment.NewLine + $"(حواله تولید: {string.Join("، ", numbers)})";
            }
        }
    }

    public async Task<CashTable<CashGroupRow>> GetGroupTableAsync(TableRequest table, string userId, bool readAll, int searchNumber, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var baseQuery = db.CashGroups.AsQueryable();
        if (!readAll)
        {
            baseQuery = baseQuery.Where(g => g.UserCreatorID == userId || g.Cashes.Any(c => c.UserCreatorID == userId));
        }
        else
        {
            baseQuery = baseQuery.Where(g => g.Cashes.Any(c => c.Status != CashStatusEnum.ثبت_موقت) || g.Cashes.Any(c => c.UserCreatorID == userId));
        }

        var query = baseQuery.Select(g => new CashGroupRow { CashGroupId = g.Id, GroupNumber = g.GroupNumber, UserCreator = g.UserCreator, CreateDate = g.CreateDate, CreateDateTxt = g.CreateDateTxt, ItemsCount = g.Cashes.Count, TotalAmount = g.Cashes.Sum(c => c.Amount + c.Fee), Status = g.Cashes.All(c => c.Status == CashStatusEnum.تایید_شده) ? CashStatusEnum.تایید_شده : CashStatusEnum.در_دست_تایید, StatusTxt = g.Cashes.All(c => c.Status == CashStatusEnum.تایید_شده) ? "تایید شده" : "در دست تایید" });
        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(f => f.GroupNumber == searchNumber || f.UserCreator.Contains(searchValue));
        }

        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
        {
            var isAscending = sortColumnDirection == "asc";
            query = sortColumn switch
            {
                "groupNumber" => isAscending ? query.OrderBy(f => f.GroupNumber) : query.OrderByDescending(f => f.GroupNumber),
                "createDateTxt" => isAscending ? query.OrderBy(f => f.CreateDate) : query.OrderByDescending(f => f.CreateDate),
                "userCreator" => isAscending ? query.OrderBy(f => f.UserCreator) : query.OrderByDescending(f => f.UserCreator),
                "itemsCount" => isAscending ? query.OrderBy(f => f.ItemsCount) : query.OrderByDescending(f => f.ItemsCount),
                "totalAmount" => isAscending ? query.OrderBy(f => f.TotalAmount) : query.OrderByDescending(f => f.TotalAmount),
                _ => query
            };
        }
        else
        {
            query = query.OrderByDescending(x => x.CreateDate);
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        var data = await query.ToListAsync(ct);
        return new(draw, recordsTotal, recordsTotal, data);
    }

    public async Task<CashTable<CashierRow>> GetCashierTableAsync(TableRequest table, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var query = db.Cashiers.AsNoTracking().GroupJoin(db.Accountancies.AsNoTracking(), c => c.UserId, ac => ac.CashierId, (c, ac) => new CashierRow { Cashier = c, CashierMappings = c.CashierExternalSoftwares.Select(m => new CashierMappingRow { SoftwareTitle = m.ExternalSoftware.Title, TafsiliCode = m.TafsiliCode }).ToList(), TotalCredit = ac.Sum(x => x.credit ?? 0), TotalDebit = ac.Sum(x => x.debit ?? 0), Balance = ac.Sum(x => (x.credit ?? 0) - (x.debit ?? 0)), IsAccepted = ac.All(x => x.Accepted == true) });
        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(x => x.Cashier.UserTxt.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn))
        {
            var asc = sortColumnDirection == "asc";
            query = sortColumn switch
            {
                "createDateTxt" => asc ? query.OrderBy(x => x.Cashier.CreateDate) : query.OrderByDescending(x => x.Cashier.CreateDate),
                "cashierTxt" => asc ? query.OrderBy(x => x.Cashier.UserTxt) : query.OrderByDescending(x => x.Cashier.UserTxt),
                _ => query.OrderByDescending(x => x.Cashier.CreateDate)};
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        var data = await query.ToListAsync(ct);
        return new(draw, recordsTotal, recordsFiltered, data);
    }

    public async Task<CashTable<BaseCash>> GetBaseTableAsync(TableRequest table, CancellationToken ct)
    {
        var draw = table.Draw;
        var start = table.Start;
        var length = table.Length;
        var sortColumn = table.SortColumn;
        var sortColumnDirection = table.SortDirection;
        var searchValue = table.Search;
        var sortDirection = table.SortDirection;
        var query = db.BaseCashes.AsNoTracking();
        var recordsTotal = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(t => t.MoeinCode.ToString().Contains(searchValue) || t.CostTitle.Contains(searchValue) || t.AccountTitle.Contains(searchValue) || t.CostItem.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        {
            var isAsc = sortDirection == "asc";
            query = sortColumn switch
            {
                "moeinCode" => isAsc ? query.OrderBy(c => c.MoeinCode) : query.OrderByDescending(c => c.MoeinCode),
                "costTitle" => isAsc ? query.OrderBy(c => c.CostTitle) : query.OrderByDescending(c => c.CostTitle),
                "accountTitle" => isAsc ? query.OrderBy(c => c.AccountTitle) : query.OrderByDescending(c => c.AccountTitle),
                _ => query.OrderByDescending(c => c.CreateDate)};
        }
        else
        {
            query = query.OrderByDescending(c => c.CreateDate);
        }

        if (length != -1)
            query = query.Skip(start).Take(length);
        var data = await query.ToListAsync(ct);
        return new(draw, recordsTotal, recordsFiltered, data);
    }
}
