using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerForm;

public sealed record GetQualityControlCustomerFormQuery(Guid CustomerId, QualityControlItemPageRequest Page)
    : IQuery<QualityControlCustomerForm?>;

public sealed class GetQualityControlCustomerFormQueryHandler(IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions)
    : IQueryHandler<GetQualityControlCustomerFormQuery, QualityControlCustomerForm?>
{
    public async Task<QualityControlCustomerForm?> HandleAsync(GetQualityControlCustomerFormQuery query, CancellationToken ct)
        => await repository.GetCustomerFormAsync(query.CustomerId, query.Page,
            await QualityControlAccessPolicy.ForAsync(await actor.GetAsync(ct), permissions, "View", ct, requireQuestions: true), ct);
}
