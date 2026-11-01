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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsTable;
public sealed record GetOrderHeadsTableQuery(TableRequest Table) : IQuery<OrderHeadTableResult>;
public sealed class GetOrderHeadsTableQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderHeadsTableQuery, OrderHeadTableResult>
{
    public async Task<OrderHeadTableResult> HandleAsync(GetOrderHeadsTableQuery request, CancellationToken ct)
    {
        return await repository.GetHeadTableAsync(request.Table, ct);
    }
}
