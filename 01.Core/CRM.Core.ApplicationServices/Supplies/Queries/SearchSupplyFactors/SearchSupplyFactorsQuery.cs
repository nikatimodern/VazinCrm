using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.SearchSupplyFactors;
public sealed record SearchSupplyFactorsQuery(SupplySelection Model) : IQuery<SupplyResult>;
public sealed class SearchSupplyFactorsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<SearchSupplyFactorsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SearchSupplyFactorsQuery query, CancellationToken cancellationToken) => repository.SearchSupplyFactorsAsync(query.Model, cancellationToken);
}
