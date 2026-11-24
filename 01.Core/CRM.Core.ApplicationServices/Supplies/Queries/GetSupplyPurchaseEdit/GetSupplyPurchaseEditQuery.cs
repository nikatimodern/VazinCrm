using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyPurchaseEdit;

public sealed record GetSupplyPurchaseEditQuery(SupplyDestinationKey Key) : IQuery<SupplyResult>;

public sealed class GetSupplyPurchaseEditQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<GetSupplyPurchaseEditQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyPurchaseEditQuery request, CancellationToken cancellationToken) =>
        integration.GetPurchaseEditAsync(request.Key, cancellationToken);
}
