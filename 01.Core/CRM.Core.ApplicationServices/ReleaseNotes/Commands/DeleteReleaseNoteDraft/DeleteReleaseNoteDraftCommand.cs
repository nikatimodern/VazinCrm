using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.DeleteReleaseNoteDraft;
public sealed record DeleteReleaseNoteDraftCommand(Guid Id) : ICommand<ReleaseNoteResult>;
public sealed class DeleteReleaseNoteDraftCommandHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : ICommandHandler<DeleteReleaseNoteDraftCommand, ReleaseNoteResult>
{
    public async Task<ReleaseNoteResult> HandleAsync(DeleteReleaseNoteDraftCommand command, CancellationToken ct)
    {
        await access.DemandAdminAsync(ct);
        return ReleaseNoteAdministration.Map(await repository.DeleteDraftAsync(command.Id, ct));
    }
}
