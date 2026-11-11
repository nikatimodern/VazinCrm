using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.CreateReleaseNote;
public sealed record CreateReleaseNoteCommand(ReleaseNoteDraft Draft) : ICommand<ReleaseNoteResult>;
public sealed class CreateReleaseNoteCommandHandler(ReleaseNoteAdministration service) : ICommandHandler<CreateReleaseNoteCommand, ReleaseNoteResult>
{
    public Task<ReleaseNoteResult> HandleAsync(CreateReleaseNoteCommand command, CancellationToken ct) => service.SaveAsync(command.Draft, null, ct);
}
