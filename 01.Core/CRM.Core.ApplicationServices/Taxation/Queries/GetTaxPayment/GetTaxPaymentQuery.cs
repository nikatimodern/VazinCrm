using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPayment;

public sealed record GetTaxPaymentQuery(Guid Id) : IQuery<TaxPaymentState>;

public sealed class GetTaxPaymentQueryHandler(ITaxPaymentRepository repository) : IQueryHandler<GetTaxPaymentQuery, TaxPaymentState>
{
    public Task<TaxPaymentState> HandleAsync(GetTaxPaymentQuery request, CancellationToken ct) =>
        repository.GetStateAsync(request.Id, ct);
}
