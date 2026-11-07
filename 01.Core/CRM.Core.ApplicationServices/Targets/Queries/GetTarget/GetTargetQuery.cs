using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Targets.Services;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;

namespace CRM.Core.ApplicationServices.Targets.Queries.GetTarget;

public sealed record GetTargetQuery(Guid Id) : IQuery<TargetPlanDetails?>;

public sealed class GetTargetQueryHandler(ITargetRepository repository, CurrentActor actor)
    : IQueryHandler<GetTargetQuery, TargetPlanDetails?>
{
    public async Task<TargetPlanDetails?> HandleAsync(GetTargetQuery query, CancellationToken ct)
    {
        TargetAccessPolicy.RequireManager(await actor.GetAsync(ct));
        return await repository.GetAsync(query.Id, ct);
    }
}
