using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetDealSupplies;
public sealed record GetDealSuppliesQuery(Guid DealId) : IQuery<SupplyResult>;
public sealed class GetDealSuppliesQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetDealSuppliesQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetDealSuppliesQuery query, CancellationToken cancellationToken) => repository.GetDealSuppliesAsync(query.DealId, cancellationToken);
}
