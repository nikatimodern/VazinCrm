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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsByPreFactor;
public sealed record GetOrderHeadsByPreFactorQuery(Guid Id, bool Undelivered = false) : IQuery<List<OrderHead>>;
public sealed class GetOrderHeadsByPreFactorQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderHeadsByPreFactorQuery, List<OrderHead>>
{
    public async Task<List<OrderHead>> HandleAsync(GetOrderHeadsByPreFactorQuery request, CancellationToken ct)
    {
        return await repository.GetHeadsByPreFactorAsync(request.Id, request.Undelivered, ct);
    }
}
