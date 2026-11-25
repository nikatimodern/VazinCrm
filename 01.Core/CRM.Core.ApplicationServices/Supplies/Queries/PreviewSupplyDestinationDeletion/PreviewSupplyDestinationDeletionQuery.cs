using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.PreviewSupplyDestinationDeletion;

public sealed record PreviewSupplyDestinationDeletionQuery(SupplyDestinationKey Key) : IQuery<SupplyResult>;

public sealed class PreviewSupplyDestinationDeletionQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<PreviewSupplyDestinationDeletionQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(PreviewSupplyDestinationDeletionQuery request, CancellationToken cancellationToken) =>
        integration.PreviewDestinationDeletionAsync(request.Key, cancellationToken);
}
