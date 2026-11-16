using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.PrepareSupplyFinancial;

public sealed record PrepareSupplyFinancialQuery(SupplyFinancialRequest Input) : IQuery<SupplyResult>;

public sealed class PrepareSupplyFinancialQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<PrepareSupplyFinancialQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(PrepareSupplyFinancialQuery request, CancellationToken cancellationToken) =>
        integration.PrepareAsync(request.Input, cancellationToken);
}
