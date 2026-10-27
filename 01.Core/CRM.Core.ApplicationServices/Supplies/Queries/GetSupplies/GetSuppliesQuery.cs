using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplies;
public sealed record GetSuppliesQuery(Guid PfId) : IQuery<SupplyResult>;
public sealed class GetSuppliesQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSuppliesQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSuppliesQuery query, CancellationToken cancellationToken) => repository.GetSuppliesAsync(query.PfId, cancellationToken);
}
