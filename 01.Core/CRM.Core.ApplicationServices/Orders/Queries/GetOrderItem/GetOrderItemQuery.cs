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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderItem;
public sealed record GetOrderItemQuery(Guid Id) : IQuery<Order?>;
public sealed class GetOrderItemQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderItemQuery, Order?>
{
    public async Task<Order?> HandleAsync(GetOrderItemQuery request, CancellationToken ct)
    {
        return await repository.GetItemAsync(request.Id, false, ct);
    }
}
