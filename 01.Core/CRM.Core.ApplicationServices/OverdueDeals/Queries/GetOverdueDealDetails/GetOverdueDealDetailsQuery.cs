using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDealDetails;
public sealed record GetOverdueDealDetailsQuery(Guid Id) : IQuery<OverdueDealDetails?>;
public sealed class GetOverdueDealDetailsQueryHandler(IOverdueDealRepository repository, CurrentActor actor) : IQueryHandler<GetOverdueDealDetailsQuery, OverdueDealDetails?>
{
    public async Task<OverdueDealDetails?> HandleAsync(GetOverdueDealDetailsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var own = current.HasRole("کارشناس فروش") && !current.HasRole("مدیر فروش", "مدیر ارشد", "مدیر عامل");
        return await repository.GetDetailsAsync(request.Id, DateTime.Now.AddDays(-OverdueDealRules.OverdueAfterDays), own, current.User.Id, ct);
    }
}
