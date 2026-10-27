using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactorAllocations;
public sealed record GetSupplyFactorAllocationsQuery(Guid Id) : IQuery<SupplyResult>;
public sealed class GetSupplyFactorAllocationsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyFactorAllocationsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyFactorAllocationsQuery query, CancellationToken cancellationToken) => repository.GetSupplyFactorAllocationsAsync(query.Id, cancellationToken);
}
