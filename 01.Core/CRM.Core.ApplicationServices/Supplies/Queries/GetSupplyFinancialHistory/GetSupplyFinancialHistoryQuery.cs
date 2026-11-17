using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFinancialHistory;

public sealed record GetSupplyFinancialHistoryQuery(Guid SourceId) : IQuery<SupplyResult>;

public sealed class GetSupplyFinancialHistoryQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<GetSupplyFinancialHistoryQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyFinancialHistoryQuery request, CancellationToken cancellationToken) =>
        integration.GetHistoryAsync(request.SourceId, cancellationToken);
}
