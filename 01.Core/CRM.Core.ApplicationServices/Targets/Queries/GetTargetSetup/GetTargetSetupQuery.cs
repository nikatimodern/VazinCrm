using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.ApplicationServices.Targets.Services;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;

namespace CRM.Core.ApplicationServices.Targets.Queries.GetTargetSetup;

public sealed record GetTargetSetupQuery : IQuery<TargetSetup>;

public sealed class GetTargetSetupQueryHandler(ITargetRepository repository, CurrentActor actor)
    : IQueryHandler<GetTargetSetupQuery, TargetSetup>
{
    public async Task<TargetSetup> HandleAsync(GetTargetSetupQuery query, CancellationToken ct)
    {
        TargetAccessPolicy.RequireManager(await actor.GetAsync(ct));
        return new TargetSetup
        {
            Metrics = await repository.GetMetricsAsync(ct),
            Experts = await repository.GetExpertsAsync(AppRoles.SalesExpert, ct)
        };
    }
}
