using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.UpdateReleaseNote;
public sealed record UpdateReleaseNoteCommand(Guid Id, ReleaseNoteDraft Draft) : ICommand<ReleaseNoteResult>;
public sealed class UpdateReleaseNoteCommandHandler(ReleaseNoteAdministration service) : ICommandHandler<UpdateReleaseNoteCommand, ReleaseNoteResult>
{
    public Task<ReleaseNoteResult> HandleAsync(UpdateReleaseNoteCommand command, CancellationToken ct) => service.SaveAsync(command.Draft, command.Id, ct);
}
