#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.AccessCatalog.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Carizos.Repositories;
public sealed class CarizoRepository(CRMContextDb db, IDocumentReadAccess access, IAccessScopeService scopes, UserContextDb users) : ICarizoRepository
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

    public async Task<bool> CanPropagateStatusAsync(Guid shareId, string actorId, CancellationToken ct)
    {
        if (!access.IsEnforced("Carizo")) return true;
        if (shareId == Guid.Empty) return false;
        var scope = await scopes.GetEffectiveScopeAsync(actorId, "Carizo.ChangeStatus", ct);
        if (scope != AccessScopes.Own)
            return await CanMutateSharedAsync(shareId, "ChangeStatus", ct);
        // A participant completing their own assigned task can trigger the legacy
        // coordinated group status transition, not arbitrary child edits or deletion.
        var root = await db.Carizos.AsNoTracking().Where(t => !t.IsDeleted && t.Id == shareId && t.ShareTaskId == shareId)
            .Select(t => new { t.UserCreatorID, t.AssignUserId }).FirstOrDefaultAsync(ct);
        if (root == null) return false;
        var group = db.Carizos.AsNoTracking().Where(t => !t.IsDeleted && (t.Id == shareId || t.ShareTaskId == shareId));
        return !await group.AnyAsync(t => t.UserCreatorID != root.UserCreatorID, ct);
    }

    public async Task<Dictionary<Guid, string>> ResolveParticipantsAsync(Guid[] userIds, CancellationToken ct)
    {
        var ids = userIds.Select(x => x.ToString()).ToArray();
        var result = await users.Users.AsNoTracking()
            .Where(u => u.IsEmployee && !u.OrganAdmin && ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName }).ToListAsync(ct);
        return result.Where(u => Guid.TryParse(u.Id, out _))
            .ToDictionary(u => Guid.Parse(u.Id), u => (u.FirstName + " " + u.LastName).Trim());
    }

    public async Task<Carizo?> GetAsync(Guid? id, bool tracking, CancellationToken ct)
    {
        var query = tracking ? db.Carizos.AsQueryable() : db.Carizos.AsNoTracking();
        return await query.Include(t => t.TaskUsers.Where(u => !u.IsDeleted)).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }
    public Task<List<Carizo>> GetSharedAsync(Guid? id, Guid? except, CancellationToken ct) => db.Carizos.Include(t => t.TaskUsers.Where(u => !u.IsDeleted)).Where(t => id.HasValue && id != Guid.Empty && !t.IsDeleted && t.ShareTaskId == id && (!except.HasValue || t.Id != except)).ToListAsync(ct);
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
