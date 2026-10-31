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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableEnvelope;
public sealed record GetCartableEnvelopeQuery() : IQuery<object>;
public sealed class GetCartableEnvelopeQueryHandler(ICartableRepository repository, CurrentActor actor, ICartableEffects effects) : IQueryHandler<GetCartableEnvelopeQuery, object>
{
    public async Task<object> HandleAsync(GetCartableEnvelopeQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await effects.GetEnvelopeAsync(current.User, await repository.GetUnreadChatsAsync(current.User.Id, ct), ct);
    }
}
