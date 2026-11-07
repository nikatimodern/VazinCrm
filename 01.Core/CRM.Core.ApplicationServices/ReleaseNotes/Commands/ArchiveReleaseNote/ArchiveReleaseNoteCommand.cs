using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Entities;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.ArchiveReleaseNote;
public sealed record ArchiveReleaseNoteCommand(Guid Id) : ICommand<ReleaseNoteResult>;
public sealed class ArchiveReleaseNoteCommandHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : ICommandHandler<ArchiveReleaseNoteCommand, ReleaseNoteResult>
{
    public async Task<ReleaseNoteResult> HandleAsync(ArchiveReleaseNoteCommand command, CancellationToken ct)
    {
        await access.DemandAdminAsync(ct);
        return ReleaseNoteAdministration.Map(await repository.ChangeStatusAsync(command.Id, ReleaseNoteStatus.Archived, DateTime.UtcNow, ct));
    }
}
