using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.SearchAllocationFactorItems;
public sealed record SearchAllocationFactorItemsQuery(string Search, Guid SupplyFactorItemId, Guid? AllocationId) : IQuery<SupplyResult>;
public sealed class SearchAllocationFactorItemsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<SearchAllocationFactorItemsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SearchAllocationFactorItemsQuery query, CancellationToken cancellationToken) => repository.SearchAllocationFactorItemsAsync(query.Search, query.SupplyFactorItemId, query.AllocationId, cancellationToken);
}
