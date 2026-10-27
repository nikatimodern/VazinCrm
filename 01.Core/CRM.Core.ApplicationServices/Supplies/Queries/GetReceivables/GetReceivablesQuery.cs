using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetReceivables;
public sealed record GetReceivablesQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetReceivablesQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetReceivablesQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetReceivablesQuery query, CancellationToken cancellationToken) => repository.GetReceivablesAsync(query.Request, cancellationToken);
}
