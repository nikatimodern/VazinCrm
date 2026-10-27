using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyPayments;
public sealed record GetSupplyPaymentsQuery(SupplyTableRequest Request) : IQuery<SupplyResult>;
public sealed class GetSupplyPaymentsQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplyPaymentsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyPaymentsQuery query, CancellationToken cancellationToken) => repository.GetSupplyPaymentsAsync(query.Request, cancellationToken);
}
