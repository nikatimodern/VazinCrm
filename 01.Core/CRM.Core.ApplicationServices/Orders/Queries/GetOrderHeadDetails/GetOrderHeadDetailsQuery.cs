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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadDetails;
public sealed record OrderHeadDetails(OrderHead? OrderHead, List<PreFactorItem> PfItemsNotInOrders);
public sealed record GetOrderHeadDetailsQuery(Guid Id) : IQuery<OrderHeadDetails>;
public sealed class GetOrderHeadDetailsQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderHeadDetailsQuery, OrderHeadDetails>
{
    public async Task<OrderHeadDetails> HandleAsync(GetOrderHeadDetailsQuery request, CancellationToken ct)
    {
        var head = await repository.GetHeadAsync(request.Id, false, ct);
        return new(head, head?.Prefactor is Guid pf ? await repository.GetMissingItemsAsync(pf, ct) : []);
    }
}
