using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyReceiptEdit;

public sealed record GetSupplyReceiptEditQuery(SupplyDestinationKey Key) : IQuery<SupplyResult>;

public sealed class GetSupplyReceiptEditQueryHandler(ISupplyFinancialIntegration integration) : IQueryHandler<GetSupplyReceiptEditQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplyReceiptEditQuery request, CancellationToken cancellationToken) =>
        integration.GetReceiptEditAsync(request.Key, cancellationToken);
}
