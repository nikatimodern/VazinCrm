using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSalesFactorAllocations;
public sealed record GetSalesFactorAllocationsQuery(Guid Id) : IQuery<SupplyResult>;
public sealed class GetSalesFactorAllocationsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSalesFactorAllocationsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSalesFactorAllocationsQuery query, CancellationToken cancellationToken) => repository.GetSalesFactorAllocationsAsync(query.Id, cancellationToken);
}
