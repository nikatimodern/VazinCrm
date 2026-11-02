using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlForm;

public sealed record GetQualityControlFormQuery(Guid DealId) : IQuery<QualityControlForm?>;

public sealed class GetQualityControlFormQueryHandler(IQualityControlRepository repository, CurrentActor actor)
    : IQueryHandler<GetQualityControlFormQuery, QualityControlForm?>
{
    public async Task<QualityControlForm?> HandleAsync(GetQualityControlFormQuery query, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        return await repository.GetFormAsync(query.DealId, access, ct);
    }
}
