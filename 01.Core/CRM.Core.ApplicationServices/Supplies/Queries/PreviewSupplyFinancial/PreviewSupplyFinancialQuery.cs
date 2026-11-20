using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.PreviewSupplyFinancial;

public sealed record PreviewSupplyFinancialQuery(SupplyFinancialRequest Input) : IQuery<SupplyResult>;

public sealed class PreviewSupplyFinancialQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<PreviewSupplyFinancialQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(PreviewSupplyFinancialQuery request, CancellationToken cancellationToken) =>
        integration.PreviewAsync(request.Input, cancellationToken);
}
