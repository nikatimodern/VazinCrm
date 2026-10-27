using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyDemands;
public sealed record GetSupplyDemandsQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetSupplyDemandsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyDemandsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyDemandsQuery query, CancellationToken cancellationToken) => repository.GetSupplyDemandsAsync(query.Request, cancellationToken);
}
