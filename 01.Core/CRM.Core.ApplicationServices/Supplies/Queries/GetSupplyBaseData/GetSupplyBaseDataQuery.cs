using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyBaseData;
public sealed record GetSupplyBaseDataQuery() : IQuery<SupplyResult>;
public sealed class GetSupplyBaseDataQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyBaseDataQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyBaseDataQuery query, CancellationToken cancellationToken) => repository.GetSupplyBaseDataAsync(cancellationToken);
}
