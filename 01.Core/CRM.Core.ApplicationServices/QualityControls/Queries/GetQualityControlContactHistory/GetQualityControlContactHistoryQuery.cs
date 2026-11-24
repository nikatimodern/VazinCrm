using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlContactHistory;

public sealed record GetQualityControlContactHistoryQuery(
    Guid CustomerId, bool IncludeDeleted, int Start, int Length)
    : IQuery<QualityControlContactHistoryPage?>;

public sealed class GetQualityControlContactHistoryQueryHandler(
    IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions)
    : IQueryHandler<GetQualityControlContactHistoryQuery, QualityControlContactHistoryPage?>
{
    public async Task<QualityControlContactHistoryPage?> HandleAsync(
        GetQualityControlContactHistoryQuery query, CancellationToken ct)
    {
        var user = await actor.GetAsync(ct);
        var isAdmin = user.HasRole(AppRoles.Admin);
        return await repository.GetContactHistoryAsync(query.CustomerId,
            query.IncludeDeleted && isAdmin, query.Start, query.Length,
            await QualityControlAccessPolicy.ForAsync(user, permissions, "View", ct), isAdmin, ct);
    }
}
