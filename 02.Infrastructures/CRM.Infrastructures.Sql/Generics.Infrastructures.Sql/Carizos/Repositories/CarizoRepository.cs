#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Carizos.Repositories;
public sealed class CarizoRepository(CRMContextDb db, IDocumentReadAccess access) : ICarizoRepository
{
    public async Task<Carizo?> GetAsync(Guid? id, bool tracking, CancellationToken ct)
    {
        var query = tracking ? db.Carizos.AsQueryable() : db.Carizos.AsNoTracking();
        // Non-tracked queries also serve shared task workflows; retain the existing mutation contract.
        return await query.Include(t => t.TaskUsers).FirstOrDefaultAsync(t => t.Id == id, ct);
    }
    public Task<List<Carizo>> GetSharedAsync(Guid? id, Guid? except, CancellationToken ct) => db.Carizos.Include(t => t.TaskUsers).Where(t => t.ShareTaskId == id && (!except.HasValue || t.Id != except)).ToListAsync(ct);
    public Task<Carizo?> GetAssignedAsync(Guid? id, string userId, CancellationToken ct) => db.Carizos.FirstOrDefaultAsync(t => t.ShareTaskId == id && t.AssignUserId == userId, ct);
    public async Task<List<Carizo>> GetRemindersAsync(string userId, DateTime now, CancellationToken ct) =>
        await (await access.ApplyAsync(db.Carizos.AsNoTracking(), "View", ct)).Include(t => t.TaskUsers).Where(t => t.AssignUserId == userId && !t.TaskStatus && t.Remind == true && t.DateMiladi <= now).ToListAsync(ct);
    public async Task<List<Carizo>> GetTasksAsync(string userId, int type, int systematic, DateTime now, CancellationToken ct)
    {
        var query = (await access.ApplyAsync(db.Carizos.AsNoTracking(), "View", ct)).Include(t => t.TaskUsers).Where(t => t.AssignUserId == userId);
        if (systematic == 1)
            query = query.Where(t => t.Systematic);
        if (systematic == 2)
            query = query.Where(t => !t.Systematic);
        query = type switch
        {
            0 => query.Where(t => !t.TaskStatus && (t.DateMiladi == null || t.DateMiladi.Value.Date > now.Date)),
            1 => query.Where(t => !t.TaskStatus && t.DateMiladi.Value.Date <= now.Date),
            2 => query.Where(t => t.TaskStatus && !t.TaskHide),
            3 => query.Where(t => t.TaskStatus && t.TaskHide),
            _ => query.Where(t => false)};
        query = query.OrderByDescending(t => t.DateMiladi).ThenByDescending(t => t.CreateDate);
        if (type == 3)
            query = query.Take(300);
        return await query.ToListAsync(ct);
    }

    public async Task<TaskCardChanges> GetCardChangesAsync(Guid id, string userId, CancellationToken ct)
    {
        var root = await db.Carizos.IgnoreQueryFilters().AsNoTracking().Where(t => t.Id == id).Select(t => new { t.Id, t.ShareTaskId }).FirstOrDefaultAsync(ct);
        var group = root?.ShareTaskId is Guid share && share != Guid.Empty ? share : id;
        var tasks = await (await access.ApplyAsync(db.Carizos.AsNoTracking(), "View", ct)).Include(t => t.TaskUsers).Where(t => t.AssignUserId == userId && (t.Id == id || t.Id == group || t.ShareTaskId == group)).ToListAsync(ct);
        return new(group, tasks);
    }

    public void Add(Carizo task) => db.Carizos.Add(task);
    public void Update(Carizo task) => db.Carizos.Update(task);
    public void Remove(Carizo task) => db.Carizos.Remove(task);
    public async Task TouchDealAsync(Guid? id, DateTime now, string date, CancellationToken ct)
    {
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deal != null)
        {
            deal.ModifyDate = now;
            deal.ModifyDateTxt = date;
        }
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
