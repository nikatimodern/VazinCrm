using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Orders.Services;

namespace CRM.Core.ApplicationServices.Orders.Queries.GetVisibleOrderHeadIds;
public sealed record GetVisibleOrderHeadIdsQuery() : IQuery<List<Guid>>;
public sealed class GetVisibleOrderHeadIdsQueryHandler(IOrderRepository repository, CurrentActor actor) : IQueryHandler<GetVisibleOrderHeadIdsQuery, List<Guid>>
{
    public async Task<List<Guid>> HandleAsync(GetVisibleOrderHeadIdsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetVisibleHeadIdsAsync(OrderAccess.Reader(current), ct);
    }
}
