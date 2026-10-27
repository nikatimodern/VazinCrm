using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplierOrders;
public sealed record GetSupplierOrdersQuery() : IQuery<SupplyResult>;
public sealed class GetSupplierOrdersQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplierOrdersQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplierOrdersQuery query, CancellationToken cancellationToken) => repository.GetSupplierOrdersAsync(cancellationToken);
}
