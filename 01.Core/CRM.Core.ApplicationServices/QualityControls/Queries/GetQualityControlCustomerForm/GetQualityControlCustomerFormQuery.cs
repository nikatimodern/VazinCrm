using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerForm;

public sealed record GetQualityControlCustomerFormQuery(Guid CustomerId) : IQuery<QualityControlCustomerForm?>;

public sealed class GetQualityControlCustomerFormQueryHandler(IQualityControlRepository repository, CurrentActor actor)
    : IQueryHandler<GetQualityControlCustomerFormQuery, QualityControlCustomerForm?>
{
    public async Task<QualityControlCustomerForm?> HandleAsync(GetQualityControlCustomerFormQuery query, CancellationToken ct)
        => await repository.GetCustomerFormAsync(query.CustomerId,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), ct);
}
