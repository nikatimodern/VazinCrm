using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyDestinationSync;

public sealed record GetSupplyDestinationSyncQuery(SupplyDestinationKey Key, bool ReceiptOnly) : IQuery<SupplyResult>;

public sealed class GetSupplyDestinationSyncQueryHandler(ISupplyFinancialIntegration integration)
    : IQueryHandler<GetSupplyDestinationSyncQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyDestinationSyncQuery query, CancellationToken ct) =>
        integration.GetSyncAsync(query.Key, query.ReceiptOnly, ct);
}
