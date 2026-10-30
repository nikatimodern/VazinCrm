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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetMissingOrderItems;
public sealed record GetMissingOrderItemsQuery(Guid Id) : IQuery<List<PreFactorItem>>;
public sealed class GetMissingOrderItemsQueryHandler(IOrderRepository repository) : IQueryHandler<GetMissingOrderItemsQuery, List<PreFactorItem>>
{
    public async Task<List<PreFactorItem>> HandleAsync(GetMissingOrderItemsQuery request, CancellationToken ct)
    {
        return await repository.GetMissingItemsAsync(request.Id, ct);
    }
}
