using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerNotes;

public sealed record GetQualityControlCustomerNotesQuery(Guid CustomerId, int Start, int Length)
    : IQuery<QualityControlCustomerNotesPage?>;

public sealed class GetQualityControlCustomerNotesQueryHandler(
    IQualityControlRepository repository, CurrentActor actor)
    : IQueryHandler<GetQualityControlCustomerNotesQuery, QualityControlCustomerNotesPage?>
{
    public async Task<QualityControlCustomerNotesPage?> HandleAsync(
        GetQualityControlCustomerNotesQuery query, CancellationToken ct)
        => await repository.GetCustomerNotesAsync(query.CustomerId, query.Start, query.Length,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), ct);
}
