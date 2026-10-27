using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.SearchAllocationSupplyItems;
public sealed record SearchAllocationSupplyItemsQuery(string Search, Guid FactorItemId, Guid? AllocationId) : IQuery<SupplyResult>;
public sealed class SearchAllocationSupplyItemsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<SearchAllocationSupplyItemsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SearchAllocationSupplyItemsQuery query, CancellationToken cancellationToken) => repository.SearchAllocationSupplyItemsAsync(query.Search, query.FactorItemId, query.AllocationId, cancellationToken);
}
