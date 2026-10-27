using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetPayment;
public sealed record GetPaymentQuery(Guid? PaymentId) : IQuery<SupplyResult>;
public sealed class GetPaymentQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetPaymentQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetPaymentQuery query, CancellationToken cancellationToken) => repository.GetPaymentAsync(query.PaymentId, cancellationToken);
}
