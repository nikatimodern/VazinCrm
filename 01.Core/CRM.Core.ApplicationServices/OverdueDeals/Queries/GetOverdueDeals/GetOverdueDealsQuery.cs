using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDeals;
public sealed record GetOverdueDealsQuery(QueryFilterInput Filter, DateTime? Start, DateTime? End, string? SortBy, string? SortDirection, bool WithFollowUp) : IQuery<OverdueDealsResult>;
public sealed class GetOverdueDealsQueryHandler(IOverdueDealRepository repository, CurrentActor actor, IApplicationDates dates) : IQueryHandler<GetOverdueDealsQuery, OverdueDealsResult>
{
    public async Task<OverdueDealsResult> HandleAsync(GetOverdueDealsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var own = current.HasRole("کارشناس فروش") && !current.HasRole("مدیر فروش", "مدیر ارشد", "مدیر عامل");
        var now = DateTime.Now;
        return await repository.GetListAsync(QueryFilterResolver.Resolve(request.Filter, dates), new(now, now.AddDays(-OverdueDealRules.OverdueAfterDays), own, current.User.Id, request.Start, request.End, request.SortBy, request.SortDirection, request.WithFollowUp), ct);
    }
}
