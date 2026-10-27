using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyProfit;
public sealed record GetSupplyProfitQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetSupplyProfitQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyProfitQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyProfitQuery query, CancellationToken cancellationToken) => repository.GetSupplyProfitAsync(query.Request, cancellationToken);
}
