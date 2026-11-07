#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.ApplicationServices.Targets.Services;
using CRM.Core.Domain.Targets.Entities;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;

namespace CRM.Core.ApplicationServices.Targets.Commands.SaveTarget;

public sealed record SaveTargetCommand(TargetSaveRequest Request, bool Finalize) : ICommand<TargetSaveResult>;

public sealed class SaveTargetCommandHandler(ITargetRepository repository, CurrentActor actor)
    : ICommandHandler<SaveTargetCommand, TargetSaveResult>
{
    public async Task<TargetSaveResult> HandleAsync(SaveTargetCommand command, CancellationToken ct)
    {
        var userId = TargetAccessPolicy.RequireManager(await actor.GetAsync(ct));
        var input = command.Request;
        if (input.Year is < 1300 or > 1600 || input.Id == Guid.Empty)
            return new(400, "سال یا شناسه تارگت معتبر نیست.");
        if ((input.Yearly?.Count ?? 0) > 500 || (input.Monthly?.Count ?? 0) > 6000 ||
            (input.Assignments?.Count ?? 0) > 30000 ||
            input.Yearly == null || input.Monthly == null || input.Assignments == null)
            return new(400, "تعداد یا ساختار اطلاعات معتبر نیست.");

        TargetPlanDetails? previous = null;
        if (input.Id.HasValue)
        {
            previous = await repository.GetAsync(input.Id.Value, ct);
            if (previous == null) return new(404, "تارگت یافت نشد.");
            if (previous.Status == TargetPlanStatus.Final) return new(409, "تارگت نهایی قابل ویرایش نیست.");
            if (previous.Version != input.Version) return new(409, "اطلاعات تغییر کرده است؛ مجدداً بارگذاری کنید.");
        }

        var metrics = (await repository.GetMetricsAsync(ct)).ToDictionary(x => x.Code, StringComparer.Ordinal);
        var currentExperts = await repository.GetExpertsAsync(AppRoles.SalesExpert, ct);
        var validExperts = currentExperts.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (previous != null)
            validExperts.UnionWith(previous.Assignments.Select(x => x.UserId!).Where(x => !string.IsNullOrWhiteSpace(x)));

        static bool DuplicateKeys(IEnumerable<TargetAmountInput> rows, bool assignments) =>
            rows.GroupBy(x => (x.MetricCode, x.Month, assignments ? x.UserId : "")).Any(g => g.Count() > 1);

        if (input.Yearly.Any(x => x == null) || input.Monthly.Any(x => x == null) ||
            input.Assignments.Any(x => x == null))
            return new(400, "اطلاعات ناقص یا نامعتبر است.");

        if (DuplicateKeys(input.Yearly, false) || DuplicateKeys(input.Monthly, false) ||
            DuplicateKeys(input.Assignments, true))
            return new(400, "مقدار تکراری برای یک شاخص، ماه یا کارشناس ثبت شده است.");

        foreach (var row in input.Yearly.Concat(input.Monthly).Concat(input.Assignments))
        {
            if (row == null || !metrics.TryGetValue(row.MetricCode ?? "", out var metric) ||
                (!metric.IsActive && (previous == null || !previous.Yearly.Any(x => x.MetricCode == metric.Code))) ||
                row.Value < 0 || row.Value > 99999999999999999999999999m ||
                decimal.Round(row.Value, 2) != row.Value ||
                (metric.Kind == TargetValueKind.Count && row.Value != decimal.Truncate(row.Value)) ||
                (metric.Kind == TargetValueKind.Percentage && row.Value > 100))
                return new(400, "یکی از شاخص‌ها یا مقادیر واردشده معتبر نیست.");
        }
        if (input.Yearly.Any(x => x.Month != 0 || x.UserId != null) ||
            input.Monthly.Any(x => x.Month is < 1 or > 12 || x.UserId != null) ||
            input.Assignments.Any(x => x.Month is < 1 or > 12 || string.IsNullOrWhiteSpace(x.UserId) ||
                !validExperts.Contains(x.UserId!)))
            return new(400, "ماه یا کارشناس انتخاب‌شده معتبر نیست.");

        if (command.Finalize)
        {
            var required = metrics.Values.Where(x => x.IsActive).Select(x => x.Code).ToArray();
            var expectedExperts = validExperts.ToArray();
            if (expectedExperts.Length == 0)
                return new(400, "هیچ کارشناس فروشی برای تخصیص وجود ندارد.");

            foreach (var code in required)
            {
                var metric = metrics[code];
                var yearly = input.Yearly.Where(x => x.MetricCode == code).ToArray();
                var monthly = input.Monthly.Where(x => x.MetricCode == code).ToArray();
                var assigned = input.Assignments.Where(x => x.MetricCode == code).ToArray();
                if (yearly.Length != 1 || monthly.Length != 12 ||
                    assigned.Length != 12 * expectedExperts.Length ||
                    Enumerable.Range(1, 12).Any(month => monthly.Count(x => x.Month == month) != 1 ||
                        assigned.Count(x => x.Month == month) != expectedExperts.Length ||
                        expectedExperts.Any(userId => !assigned.Any(x => x.Month == month && x.UserId == userId))))
                    return new(400, "اهداف سالانه، ماهانه و تخصیص کارشناسان باید برای همه ماه‌ها تکمیل شوند.");

                if (metric.Kind == TargetValueKind.Percentage) continue;
                if (monthly.Sum(x => x.Value) != yearly[0].Value)
                    return new(400, $"جمع ماهانه شاخص «{metric.Title}» با تارگت سالانه برابر نیست.");
                foreach (var month in monthly)
                    if (assigned.Where(x => x.Month == month.Month).Sum(x => x.Value) != month.Value)
                        return new(400, $"مجموع تخصیص «{metric.Title}» در ماه {month.Month} با هدف ماهانه برابر نیست.");
            }
        }
        return await repository.SaveAsync(input, command.Finalize, userId, ct);
    }
}
