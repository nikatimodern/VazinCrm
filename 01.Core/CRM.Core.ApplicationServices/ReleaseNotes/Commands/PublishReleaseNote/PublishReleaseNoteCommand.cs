using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.PublishReleaseNote;
public sealed record PublishReleaseNoteCommand(Guid Id) : ICommand<ReleaseNoteResult>;
public sealed class PublishReleaseNoteCommandHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : ICommandHandler<PublishReleaseNoteCommand, ReleaseNoteResult>
{
    public async Task<ReleaseNoteResult> HandleAsync(PublishReleaseNoteCommand command, CancellationToken ct)
    {
        await access.DemandAdminAsync(ct);
        return ReleaseNoteAdministration.Map(await repository.ChangeStatusAsync(command.Id, ReleaseNoteStatus.Published, DateTime.UtcNow, ct));
    }
}
