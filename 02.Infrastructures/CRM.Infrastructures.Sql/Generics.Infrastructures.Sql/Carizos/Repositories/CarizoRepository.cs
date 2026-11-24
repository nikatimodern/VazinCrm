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
    public async Task<bool> CanAccessAsync(Guid id, string action, CancellationToken ct)
    {
        var query = await access.ApplyAsync(db.Carizos.AsNoTracking().Where(t => !t.IsDeleted && t.Id == id), "View", ct);
        if (action != "View") query = await access.ApplyAsync(query, action, ct);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> CanMutateSharedAsync(Guid? shareId, string action, CancellationToken ct)
    {
        if (!access.IsEnforced("Carizo")) return true;
        if (!shareId.HasValue || shareId.Value == Guid.Empty) return false;
        var all = db.Carizos.AsNoTracking().Where(t => !t.IsDeleted && (t.ShareTaskId == shareId || t.Id == shareId));
        var visible = await access.ApplyAsync(all, "View", ct);
        visible = await access.ApplyAsync(visible, action, ct);
        return !await all.AnyAsync(t => !visible.Any(v => v.Id == t.Id), ct);
    }

    public async Task<Carizo?> GetAsync(Guid? id, bool tracking, CancellationToken ct)
    {
        var query = tracking ? db.Carizos.AsQueryable() : db.Carizos.AsNoTracking();
        return await query.Include(t => t.TaskUsers.Where(u => !u.IsDeleted)).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }
    public Task<List<Carizo>> GetSharedAsync(Guid? id, Guid? except, CancellationToken ct) => db.Carizos.Include(t => t.TaskUsers).Where(t => t.ShareTaskId == id && (!except.HasValue || t.Id != except)).ToListAsync(ct);
    public Task<Carizo?> GetAssignedAsync(Guid? id, string userId, CancellationToken ct) => db.Carizos.FirstOrDefaultAsync(t => t.ShareTaskId == id && t.AssignUserId == userId, ct);
    public async Task<List<Carizo>> GetRemindersAsync(string userId, DateTime now, CancellationToken ct) =>
        await (await access.ApplyAsync(db.Carizos.AsNoTracking(), "View", ct)).Include(t => t.TaskUsers).Where(t => t.AssignUserId == userId && !t.TaskStatus && t.Remind == true && t.DateMiladi <= now).ToListAsync(ct);
    public async Task<List<Carizo>> GetTasksAsync(string userId, int type, int systematic, DateTime now, CancellationToken ct)
    {
        var query = (await access.ApplyAsync(db.Carizos.AsNoTracking().Where(t => !t.IsDeleted), "View", ct)).Include(t => t.TaskUsers.Where(u => !u.IsDeleted)).AsQueryable();
        if (!access.IsEnforced("Carizo")) query = query.Where(t => t.AssignUserId == userId);
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
        var roots = await access.ApplyAsync(db.Carizos.AsNoTracking().Where(t => !t.IsDeleted && t.Id == id), "View", ct);
        var root = await roots.Select(t => new { t.Id, t.ShareTaskId }).FirstOrDefaultAsync(ct);
        var group = root?.ShareTaskId is Guid share && share != Guid.Empty ? share : id;
        var query = (await access.ApplyAsync(db.Carizos.AsNoTracking().Where(t => !t.IsDeleted), "View", ct)).Include(t => t.TaskUsers.Where(u => !u.IsDeleted)).Where(t => t.Id == id || t.Id == group || t.ShareTaskId == group);
        if (!access.IsEnforced("Carizo")) query = query.Where(t => t.AssignUserId == userId);
        var tasks = await query.ToListAsync(ct);
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
