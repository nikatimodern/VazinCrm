#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Querying;
using CRM.Infrastructures.Sql.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.Orders.Repositories;
public sealed class OrderRepository(CRMContextDb db) : IOrderRepository, IAsyncDisposable
{
    private IDbContextTransaction? transaction;
    private IQueryable<OrderHead> Visible(IQueryable<OrderHead> query, OrderReader reader)
    {
        if (reader.ReadAll)
            return query;
        var id = Guid.Parse(reader.UserId);
        return reader.ReadAssigned ? query.Where(h => h.UserCreatorID == reader.UserId || h.UserTaminId == id) : query.Where(h => h.UserCreatorID == reader.UserId);
    }

    public Task<List<OrderHead>> GetHeadsByPreFactorAsync(Guid id, bool undelivered, CancellationToken ct) => db.OrderHeads.AsNoTracking().Where(h => h.Prefactor == id && (!undelivered || h.Status != OrderHeadStatusEnum.تحویل_شده)).Include(h => h.Orders).ToListAsync(ct);
    public Task<List<OrderHead>> GetHeadsByDealAsync(Guid id, CancellationToken ct) => db.OrderHeads.AsNoTracking().Where(h => h.DealId == id).Include(h => h.Deal).ToListAsync(ct);
    public Task<List<OrderOption>> GetPreFactorOptionsAsync(Guid id, CancellationToken ct) => db.PreFactors.AsNoTracking().Where(p => p.CustomerId == id && p.Pftype == (int)PreFactorEnum.در_انتظار_تولید).Select(p => new OrderOption(p.Id, p.SerialNumber.ToString())).ToListAsync(ct);
    public Task<List<OrderOption>> GetFactorOptionsAsync(Guid id, CancellationToken ct) => db.Factors.AsNoTracking().Where(p => p.CustomerId == id).Select(p => new OrderOption(p.Id, p.SerialNumber.ToString())).ToListAsync(ct);
    public Task<List<OrderOption>> GetCustomerOptionsAsync(OrderReader reader, CancellationToken ct)
    {
        var id = Guid.Parse(reader.UserId);
        return db.Customers.AsNoTracking().Where(c => ((int)c.RoleType == 1 || (int)c.RoleType == 3) && db.UserCustomers.Any(u => u.CustomerId == c.Id && (reader.ReadAll || u.UserId == id))).Select(c => new OrderOption(c.Id, c.CompanyName + " " + c.AgentName + " _ (کد مشتری  " + c.CustomerCode + ")")).ToListAsync(ct);
    }

    public Task<List<OrderHead>> GetBoardAsync(QueryFilter filter, int invoiceStatus, OrderReader reader, CancellationToken ct)
    {
        var query = db.OrderHeads.AsNoTracking().Include(h => h.RequestAmountOrderHeads).OrderByDescending(h => h.CreateDate).AsQueryable();
        if (!string.IsNullOrEmpty(filter.Search))
        {
            var val = filter.Search;
            query = query.Where(h => h.CustomerTxt.Contains(val) || h.OrderNumber.ToString() == val || h.PrefactorSer.ToString() == val || h.CustomerCode.ToString() == val);
        }

        if (invoiceStatus == 1)
            query = query.Where(h => db.PreFactors.Any(p => p.Id == h.Prefactor && p.Factors.Count > 0));
        if (invoiceStatus == 2)
            query = query.Where(h => !db.PreFactors.Any(p => p.Id == h.Prefactor && p.Factors.Count > 0));
        query = Visible(query.ApplyFilter(filter, h => h.Customer, h => h.OrderDateMildi, h => h.UserCreatorID), reader);
        if (!filter.HasFilter && string.IsNullOrEmpty(filter.Search))
            query = query.Where(h => h.OrderDateMildi > new DateTime(2025, 3, 21)).Take(200);
        return query.Include(h => h.Deal).ToListAsync(ct);
    }

    public Task<OrderHead?> GetHeadAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.OrderHeads : db.OrderHeads.AsNoTracking()).Include(h => h.Orders).FirstOrDefaultAsync(h => h.Id == id, ct);
    public Task<Order?> GetItemAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Orders : db.Orders.AsNoTracking()).FirstOrDefaultAsync(o => o.Id == id, ct);
    public Task<List<PreFactorItem>> GetMissingItemsAsync(Guid pfId, CancellationToken ct) => db.PreFactorItems.AsNoTracking().Where(i => i.PrefactorId == pfId && i.IsInProduction && !db.OrderHeads.Where(h => h.Prefactor == pfId).SelectMany(h => h.Orders).Any(o => o.PreFactorItemId == i.Id)).ToListAsync(ct);
    public Task<List<Guid>> GetVisibleHeadIdsAsync(OrderReader reader, CancellationToken ct) => Visible(db.OrderHeads.AsNoTracking(), reader).Select(h => h.Id).ToListAsync(ct);
    public Task<List<OrderHead>> SelectHeadsAsync(HeadSelection selection, CancellationToken ct)
    {
        var query = db.OrderHeads.AsNoTracking().AsQueryable();
        if (selection.FilterIds != null)
            query = query.Where(h => selection.FilterIds.Contains(h.Id));
        query = query.OrderByDescending(h => selection.SelectedIds.Contains(h.Id)).ThenByDescending(h => h.CreateDate);
        if (!string.IsNullOrEmpty(selection.Search))
        {
            var val = selection.Search;
            var number = int.TryParse(val, out var parsed) ? parsed : 0;
            query = query.Where(h => h.CustomerTxt.Contains(val) || h.OrderNumber == number || h.PrefactorSer == number || h.CustomerCode == number);
        }
        else
            query = query.Take(50);
        return query.ToListAsync(ct);
    }

    public Task<bool> AllowMultipleSelectionAsync(CancellationToken ct) => db.ASettings.Select(s => s.MultiSelectOrderHeadsInRequestAmount).FirstOrDefaultAsync(ct);
    public async Task<OrderTableResult> GetItemTableAsync(Guid id, TableRequest request, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.OrderHeadId == id);
        var count = await query.CountAsync(ct);
        query = request.SortDirection == "asc" ? query.OrderBy(o => o.CreateDate) : query.OrderByDescending(o => o.CreateDate);
        return new(request.Draw, count, count, await query.Skip(request.Start).Take(request.Length).ToListAsync(ct));
    }

    public async Task<OrderHeadTableResult> GetHeadTableAsync(TableRequest request, CancellationToken ct)
    {
        var query = db.OrderHeads.AsNoTracking();
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(h => h.CustomerTxt.Contains(request.Search));
        var count = await query.CountAsync(ct);
        query = (request.SortColumn, request.SortDirection) switch
        {
            ("OrderNumber", "asc") => query.OrderBy(h => h.OrderNumber),
            ("OrderNumber", _) => query.OrderByDescending(h => h.OrderNumber),
            (_, "asc") => query.OrderBy(h => h.CreateDate),
            _ => query.OrderByDescending(h => h.CreateDate)};
        if (request.Length != -1)
            query = query.Skip(request.Start).Take(request.Length);
        return new(request.Draw, count, count, await query.ToListAsync(ct));
    }

    public async Task<OrderPrintData> GetPrintDataAsync(Guid id, CancellationToken ct)
    {
        var company = await db.Companies.AsNoTracking().Include(c => c.CompanyFiles).FirstOrDefaultAsync(ct);
        var head = await GetHeadAsync(id, false, ct);
        var pf = head == null ? null : await db.PreFactors.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == head.Prefactor, ct);
        return new(company, head, pf);
    }

    public async Task<string?> GetCustomerMobileAsync(OrderHead head, CancellationToken ct)
    {
        return head.Customer.HasValue ? await db.Customers.Where(c => c.Id == head.Customer).Select(c => c.Mobile).FirstOrDefaultAsync(ct) : await db.Deals.Where(d => d.Id == head.DealId).Select(d => d.MobileNumber).FirstOrDefaultAsync(ct);
    }

    public async Task SaveHeadAsync(OrderHead head, bool create, CancellationToken ct)
    {
        if (create)
        {
            db.OrderHeads.Add(head);
            return;
        }

        var current = await db.OrderHeads.Include(h => h.Orders).FirstAsync(h => h.Id == head.Id, ct);
        db.Entry(current).CurrentValues.SetValues(head);
        db.Entry(current).Property(h => h.OrderNumber).IsModified = false;
        foreach (var item in head.Orders ?? [])
        {
            var existing = current.Orders.FirstOrDefault(i => i.Id == item.Id);
            if (existing == null)
            {
                item.OrderHeadId = current.Id;
                current.Orders.Add(item);
            }
            else
                db.Entry(existing).CurrentValues.SetValues(item);
        }
    }

    public async Task SaveItemAsync(Order item, bool create, CancellationToken ct)
    {
        if (create)
            db.Orders.Add(item);
        else
        {
            var current = await db.Orders.FirstAsync(o => o.Id == item.Id, ct);
            db.Entry(current).CurrentValues.SetValues(item);
            foreach (var field in new[]
            {
                "CreateDate",
                "CreateDateTxt"
            }

            )
                db.Entry(current).Property(field).IsModified = false;
        }
    }

    public async Task TouchDealAsync(Guid id, DateTime time, string text, CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deal != null)
        {
            deal.ModifyDate = time;
            deal.ModifyDateTxt = text;
        }
    }

    public async Task BeginAsync(CancellationToken ct)
    {
        transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task CommitAsync(CancellationToken ct)
    {
        if (transaction != null)
            await transaction.CommitAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
    }
}
