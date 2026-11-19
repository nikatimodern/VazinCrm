using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.AddQualityControlCustomerNote;

public sealed record AddQualityControlCustomerNoteCommand(AddQualityControlCustomerNoteRequest Request)
    : ICommand<AddQualityControlCustomerNoteResult>;

public sealed class AddQualityControlCustomerNoteCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<AddQualityControlCustomerNoteCommand, AddQualityControlCustomerNoteResult>
{
    public async Task<AddQualityControlCustomerNoteResult> HandleAsync(
        AddQualityControlCustomerNoteCommand command, CancellationToken ct)
        => await repository.AddCustomerNoteAsync(command.Request,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), dates.PersianNow(), ct);
}
