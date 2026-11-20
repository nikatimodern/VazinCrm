using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.UpdateQualityControlCustomerNote;

public sealed record UpdateQualityControlCustomerNoteCommand(UpdateQualityControlCustomerNoteRequest Request)
    : ICommand<QualityControlCustomerNoteChangeResult>;

public sealed class UpdateQualityControlCustomerNoteCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<UpdateQualityControlCustomerNoteCommand, QualityControlCustomerNoteChangeResult>
{
    public async Task<QualityControlCustomerNoteChangeResult> HandleAsync(
        UpdateQualityControlCustomerNoteCommand command, CancellationToken ct)
    {
        var input = command.Request;
        if (input.CustomerId == Guid.Empty || input.NoteId == Guid.Empty
            || input.RowVersion is not { Length: 8 }
            || string.IsNullOrWhiteSpace(input.Description) || input.Description.Length > 4000)
            return new(400, "اطلاعات ویرایش یادداشت معتبر نیست.");
        return await repository.UpdateCustomerNoteAsync(input,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), dates.PersianNow(), ct);
    }
}
