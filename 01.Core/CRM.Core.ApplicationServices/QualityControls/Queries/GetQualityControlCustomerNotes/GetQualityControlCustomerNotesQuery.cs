using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerNotes;

public sealed record GetQualityControlCustomerNotesQuery(Guid CustomerId, int Start, int Length)
    : IQuery<QualityControlCustomerNotesPage?>;

public sealed class GetQualityControlCustomerNotesQueryHandler(
    IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions)
    : IQueryHandler<GetQualityControlCustomerNotesQuery, QualityControlCustomerNotesPage?>
{
    public async Task<QualityControlCustomerNotesPage?> HandleAsync(
        GetQualityControlCustomerNotesQuery query, CancellationToken ct)
        => await repository.GetCustomerNotesAsync(query.CustomerId, query.Start, query.Length,
            await QualityControlAccessPolicy.ForAsync(await actor.GetAsync(ct), permissions, "View", ct), ct);
}
