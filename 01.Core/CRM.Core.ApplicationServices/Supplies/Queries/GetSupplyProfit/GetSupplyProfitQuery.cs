using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.Supplies.Services;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyProfit;
public sealed record GetSupplyProfitQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetSupplyProfitQueryHandler(ISupplyQueryRepository repository, IApplicationDates dates) : IQueryHandler<GetSupplyProfitQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyProfitQuery query, CancellationToken cancellationToken) => repository.GetSupplyProfitAsync(SupplyFilterResolver.Resolve(query.Request, dates), cancellationToken);
}
