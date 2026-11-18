using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.SearchSupplyFinancialOptions;

public sealed record SearchSupplyFinancialOptionsQuery(SupplyFinancialRequest Input, bool Items) : IQuery<SupplyResult>;

public sealed class SearchSupplyFinancialOptionsQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<SearchSupplyFinancialOptionsQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SearchSupplyFinancialOptionsQuery request, CancellationToken cancellationToken) =>
        integration.SearchAsync(request.Input, request.Items, cancellationToken);
}
