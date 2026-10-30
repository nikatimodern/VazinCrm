using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.Supplies.Services;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyDemands;
public sealed record GetSupplyDemandsQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetSupplyDemandsQueryHandler(ISupplyQueryRepository repository, IApplicationDates dates) : IQueryHandler<GetSupplyDemandsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyDemandsQuery query, CancellationToken cancellationToken) => repository.GetSupplyDemandsAsync(SupplyFilterResolver.Resolve(query.Request, dates), cancellationToken);
}
