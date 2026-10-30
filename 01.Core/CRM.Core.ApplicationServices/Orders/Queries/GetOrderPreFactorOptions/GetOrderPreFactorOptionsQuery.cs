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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorOptions;
public sealed record GetOrderPreFactorOptionsQuery(Guid Id) : IQuery<List<OrderOption>>;
public sealed class GetOrderPreFactorOptionsQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderPreFactorOptionsQuery, List<OrderOption>>
{
    public async Task<List<OrderOption>> HandleAsync(GetOrderPreFactorOptionsQuery request, CancellationToken ct)
    {
        return await repository.GetPreFactorOptionsAsync(request.Id, ct);
    }
}
