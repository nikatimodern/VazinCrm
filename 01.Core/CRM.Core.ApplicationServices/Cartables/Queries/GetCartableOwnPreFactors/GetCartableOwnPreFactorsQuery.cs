using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableOwnPreFactors;
public sealed record GetCartableOwnPreFactorsQuery() : IQuery<List<PreFactor>>;
public sealed class GetCartableOwnPreFactorsQueryHandler(ICartableRepository repository, CurrentActor actor) : IQueryHandler<GetCartableOwnPreFactorsQuery, List<PreFactor>>
{
    public async Task<List<PreFactor>> HandleAsync(GetCartableOwnPreFactorsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetOwnPreFactorsAsync(current.User.Id, ct);
    }
}
