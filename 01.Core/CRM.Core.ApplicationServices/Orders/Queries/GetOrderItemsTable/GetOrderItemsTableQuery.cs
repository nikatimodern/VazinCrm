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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderItemsTable;
public sealed record GetOrderItemsTableQuery(Guid Id, TableRequest Table) : IQuery<OrderTableResult>;
public sealed class GetOrderItemsTableQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderItemsTableQuery, OrderTableResult>
{
    public async Task<OrderTableResult> HandleAsync(GetOrderItemsTableQuery request, CancellationToken ct)
    {
        var result = await repository.GetItemTableAsync(request.Id, request.Table, ct);
        for (var i = 0; i < result.Data.Count; i++)
            result.Data[i].Row = request.Table.Start + i + 1;
        return result;
    }
}
