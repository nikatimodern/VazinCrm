using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomers;

public sealed record GetQualityControlCustomersQuery(QualityControlCustomerListRequest Request)
    : IQuery<QualityControlCustomerListResult>;

public sealed class GetQualityControlCustomersQueryHandler(IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions)
    : IQueryHandler<GetQualityControlCustomersQuery, QualityControlCustomerListResult>
{
    public async Task<QualityControlCustomerListResult> HandleAsync(GetQualityControlCustomersQuery query, CancellationToken ct)
        => await repository.GetCustomersAsync(query.Request,
            await QualityControlAccessPolicy.ForAsync(await actor.GetAsync(ct), permissions, "View", ct), ct);
}
