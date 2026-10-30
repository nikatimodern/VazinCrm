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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorHeads;
public sealed record PreFactorOrderHeads(List<OrderHead> OrderHeads, List<PreFactorItem> PfItemsNotInOrders);
public sealed record GetOrderPreFactorHeadsQuery(Guid Id) : IQuery<PreFactorOrderHeads>;
public sealed class GetOrderPreFactorHeadsQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderPreFactorHeadsQuery, PreFactorOrderHeads>
{
    public async Task<PreFactorOrderHeads> HandleAsync(GetOrderPreFactorHeadsQuery request, CancellationToken ct)
    {
        var heads = await repository.GetHeadsByPreFactorAsync(request.Id, false, ct);
        return new(heads, heads.Count > 0 ? await repository.GetMissingItemsAsync(request.Id, ct) : []);
    }
}
