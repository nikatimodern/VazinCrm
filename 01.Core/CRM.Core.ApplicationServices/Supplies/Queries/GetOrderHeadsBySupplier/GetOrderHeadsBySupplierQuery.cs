using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetOrderHeadsBySupplier;
public sealed record GetOrderHeadsBySupplierQuery(Guid SupplierId) : IQuery<SupplyResult>;
public sealed class GetOrderHeadsBySupplierQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetOrderHeadsBySupplierQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetOrderHeadsBySupplierQuery query, CancellationToken cancellationToken) => repository.GetOrderHeadsBySupplierAsync(query.SupplierId, cancellationToken);
}
