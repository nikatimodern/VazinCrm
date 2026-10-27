using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Queries.GetSupplierFinancialSummary;
public sealed record GetSupplierFinancialSummaryQuery(Guid SupplierId) : IQuery<SupplyResult>;
public sealed class GetSupplierFinancialSummaryQueryHandler(ISupplyQueryRepository repository) : IQueryHandler<GetSupplierFinancialSummaryQuery, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(GetSupplierFinancialSummaryQuery query, CancellationToken cancellationToken) => repository.GetSupplierFinancialSummaryAsync(query.SupplierId, cancellationToken);
}
