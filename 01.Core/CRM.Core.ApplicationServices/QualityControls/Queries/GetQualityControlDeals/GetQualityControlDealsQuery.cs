using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlDeals;

public sealed record GetQualityControlDealsQuery(QualityControlListRequest Request) : IQuery<QualityControlListResult>;

public sealed class GetQualityControlDealsQueryHandler(IQualityControlRepository repository, CurrentActor actor)
    : IQueryHandler<GetQualityControlDealsQuery, QualityControlListResult>
{
    public async Task<QualityControlListResult> HandleAsync(GetQualityControlDealsQuery query, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        return await repository.GetDealsAsync(query.Request, access, ct);
    }
}
