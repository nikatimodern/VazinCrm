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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderCustomerOptions;
public sealed record GetOrderCustomerOptionsQuery() : IQuery<List<OrderOption>>;
public sealed class GetOrderCustomerOptionsQueryHandler(IOrderRepository repository, CurrentActor actor) : IQueryHandler<GetOrderCustomerOptionsQuery, List<OrderOption>>
{
    public async Task<List<OrderOption>> HandleAsync(GetOrderCustomerOptionsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetCustomerOptionsAsync(OrderAccess.Reader(current), ct);
    }
}
