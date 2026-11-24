using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.DeleteQualityControlCustomerNote;

public sealed record DeleteQualityControlCustomerNoteCommand(DeleteQualityControlCustomerNoteRequest Request)
    : ICommand<QualityControlCustomerNoteChangeResult>;

public sealed class DeleteQualityControlCustomerNoteCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions, IApplicationDates dates)
    : ICommandHandler<DeleteQualityControlCustomerNoteCommand, QualityControlCustomerNoteChangeResult>
{
    public async Task<QualityControlCustomerNoteChangeResult> HandleAsync(
        DeleteQualityControlCustomerNoteCommand command, CancellationToken ct)
    {
        var input = command.Request;
        if (input.CustomerId == Guid.Empty || input.NoteId == Guid.Empty
            || input.RowVersion is not { Length: 8 })
            return new(400, "اطلاعات حذف یادداشت معتبر نیست.");
        return await repository.DeleteCustomerNoteAsync(input,
            await QualityControlAccessPolicy.ForAsync(await actor.GetAsync(ct), permissions, "Delete", ct), dates.PersianNow(), ct);
    }
}
