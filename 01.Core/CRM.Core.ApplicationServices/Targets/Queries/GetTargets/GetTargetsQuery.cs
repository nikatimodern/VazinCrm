using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Targets.Services;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;

namespace CRM.Core.ApplicationServices.Targets.Queries.GetTargets;

public sealed record GetTargetsQuery : IQuery<IReadOnlyList<TargetPlanSummary>>;

public sealed class GetTargetsQueryHandler(ITargetRepository repository, CurrentActor actor)
    : IQueryHandler<GetTargetsQuery, IReadOnlyList<TargetPlanSummary>>
{
    public async Task<IReadOnlyList<TargetPlanSummary>> HandleAsync(GetTargetsQuery query, CancellationToken ct)
    {
        TargetAccessPolicy.RequireManager(await actor.GetAsync(ct));
        return await repository.ListAsync(ct);
    }
}
