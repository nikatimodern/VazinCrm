using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.GetTaxDispatch;

public sealed record GetTaxDispatchQuery(Guid Id, long? HistoryBefore = null) : IQuery<TaxDispatchDetails>;

public sealed class GetTaxDispatchQueryHandler(ITaxInvoiceListing listing)
    : IQueryHandler<GetTaxDispatchQuery, TaxDispatchDetails>
{
    public Task<TaxDispatchDetails> HandleAsync(GetTaxDispatchQuery query, CancellationToken ct) =>
        listing.ReadDispatchAsync(query.Id, ct, query.HistoryBefore);
}
