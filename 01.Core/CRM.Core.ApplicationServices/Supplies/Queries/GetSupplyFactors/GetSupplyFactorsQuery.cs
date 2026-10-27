using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactors;
public sealed record GetSupplyFactorsQuery(string Search, Guid? DealId) : IQuery<SupplyResult>;
public sealed class GetSupplyFactorsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyFactorsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyFactorsQuery query, CancellationToken cancellationToken) => repository.GetSupplyFactorsAsync(query.Search, query.DealId, cancellationToken);
}
