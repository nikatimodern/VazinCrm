using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactor;
public sealed record GetSupplyFactorQuery(Guid Id) : IQuery<SupplyResult>;
public sealed class GetSupplyFactorQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyFactorQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyFactorQuery query, CancellationToken cancellationToken) => repository.GetSupplyFactorAsync(query.Id, cancellationToken);
}
