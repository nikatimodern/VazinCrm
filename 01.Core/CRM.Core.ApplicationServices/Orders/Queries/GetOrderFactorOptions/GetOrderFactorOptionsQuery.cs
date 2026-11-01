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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderFactorOptions;
public sealed record GetOrderFactorOptionsQuery(Guid Id) : IQuery<List<OrderOption>>;
public sealed class GetOrderFactorOptionsQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderFactorOptionsQuery, List<OrderOption>>
{
    public async Task<List<OrderOption>> HandleAsync(GetOrderFactorOptionsQuery request, CancellationToken ct)
    {
        return await repository.GetFactorOptionsAsync(request.Id, ct);
    }
}
