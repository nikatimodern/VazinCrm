#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Targets.Entities;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Targets.Repositories;

public sealed class TargetRepository(CRMContextDb db, UserManager<AppUser> users) : ITargetRepository
{
    public async Task<IReadOnlyList<TargetMetric>> GetMetricsAsync(CancellationToken ct) =>
        await db.TargetMetrics.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<TargetExpert>> GetExpertsAsync(string role, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var experts = await users.GetUsersInRoleAsync(role);
        return experts.OrderBy(x => x.FullName)
            .Select(x => new TargetExpert { Id = x.Id, Name = x.FullName }).ToList();
    }

    public async Task<IReadOnlyList<TargetPlanSummary>> ListAsync(CancellationToken ct)
    {
        // Keep counts and sales amount on the SQL server. Including both collections
        // multiplies their rows and makes the list increasingly slow as plans grow.
        return await db.TargetPlans.AsNoTracking()
            .OrderByDescending(x => x.Year)
            .Select(x => new TargetPlanSummary
            {
                Id = x.Id,
                Year = x.Year,
                Status = x.Status,
                Version = x.Version,
                SalesTarget = x.Values
                    .Where(v => v.Month == 0 && v.MetricCode == "sales_amount")
                    .Select(v => (decimal?)v.Value)
                    .FirstOrDefault() ?? 0m,
                AllocatedMonths = x.Assignments.Select(a => a.Month).Distinct().Count(),
                ExpertCount = x.Assignments.Select(a => a.UserId).Distinct().Count(),
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<TargetPlanDetails?> GetAsync(Guid id, CancellationToken ct)
    {
        var plan = await db.TargetPlans.AsNoTracking()
            .Include(x => x.Values).Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan == null) return null;
        return new TargetPlanDetails
        {
            Id = plan.Id, Year = plan.Year, Version = plan.Version, Status = plan.Status,
            CreatedAtUtc = plan.CreatedAtUtc, UpdatedAtUtc = plan.UpdatedAtUtc,
            Yearly = plan.Values.Where(x => x.Month == 0)
                .Select(x => new TargetAmountInput { MetricCode = x.MetricCode, Month = 0, Value = x.Value }).ToList(),
            Monthly = plan.Values.Where(x => x.Month > 0)
                .Select(x => new TargetAmountInput { MetricCode = x.MetricCode, Month = x.Month, Value = x.Value }).ToList(),
            Assignments = plan.Assignments
                .Select(x => new TargetAmountInput { MetricCode = x.MetricCode, Month = x.Month, UserId = x.UserId, Value = x.Value }).ToList()
        };
    }

    public async Task<TargetSaveResult> SaveAsync(TargetSaveRequest request, bool finalize, string actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        TargetPlan? plan = null;
        if (request.Id.HasValue)
        {
            plan = await db.TargetPlans.Include(x => x.Values).Include(x => x.Assignments)
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value, ct);
            if (plan == null) return new(404, "تارگت یافت نشد.");
            if (plan.Status == TargetPlanStatus.Final) return new(409, "این تارگت قبلاً نهایی شده است.");
            if (plan.Version != request.Version) return new(409, "اطلاعات توسط کاربر دیگری تغییر کرده است؛ دوباره بارگذاری کنید.");
        }
        else
        {
            if (await db.TargetPlans.AnyAsync(x => x.Year == request.Year, ct))
                return new(409, "برای این سال قبلاً تارگت ثبت شده است.");
            plan = new TargetPlan { Year = request.Year, CreatedById = actorId };
            db.TargetPlans.Add(plan);
        }

        if (await db.TargetPlans.AnyAsync(x => x.Year == request.Year && x.Id != plan.Id, ct))
            return new(409, "برای این سال قبلاً تارگت ثبت شده است.");

        plan.Year = request.Year;
        plan.ModifiedById = actorId;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        if (request.Id.HasValue) plan.Version++;
        if (finalize) { plan.Status = TargetPlanStatus.Final; plan.FinalizedAtUtc = DateTime.UtcNow; }

        // Update existing rows instead of deleting and re-inserting matching unique keys.
        var values = request.Yearly.Concat(request.Monthly)
            .ToDictionary(x => (x.MetricCode, x.Month));
        foreach (var stored in plan.Values)
        {
            if (values.Remove((stored.MetricCode, stored.Month), out var input))
                stored.Value = input.Value;
            else
                db.TargetValues.Remove(stored);
        }
        foreach (var input in values.Values)
            db.TargetValues.Add(new TargetValue
            {
                PlanId = plan.Id, MetricCode = input.MetricCode, Month = input.Month, Value = input.Value
            });

        var assignments = request.Assignments.ToDictionary(x => (x.MetricCode, x.Month, x.UserId));
        foreach (var stored in plan.Assignments)
        {
            if (assignments.Remove((stored.MetricCode, stored.Month, stored.UserId), out var input))
                stored.Value = input.Value;
            else
                db.TargetAssignments.Remove(stored);
        }
        foreach (var input in assignments.Values)
            db.TargetAssignments.Add(new TargetAssignment
            {
                PlanId = plan.Id, MetricCode = input.MetricCode,
                Month = input.Month, UserId = input.UserId!, Value = input.Value
            });
        db.TargetPlanAudits.Add(new TargetPlanAudit
        {
            PlanId = plan.Id, Version = plan.Version, ActorId = actorId,
            Action = finalize ? "Finalize" : request.Id.HasValue ? "UpdateDraft" : "CreateDraft"
        });

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(200, finalize ? "تارگت نهایی شد." : "پیش‌نویس ذخیره شد.", plan.Id, plan.Version);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return new(409, "تارگت توسط کاربر دیگری تغییر کرده است.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return new(409, "ذخیره اطلاعات ممکن نشد؛ وضعیت سال، شاخص‌ها و داده‌های تکراری را بررسی کنید.");
        }
    }
}
