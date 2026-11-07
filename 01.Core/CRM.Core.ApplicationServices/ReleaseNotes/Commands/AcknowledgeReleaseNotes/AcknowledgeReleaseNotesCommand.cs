using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
namespace CRM.Core.ApplicationServices.ReleaseNotes.Commands.AcknowledgeReleaseNotes;
public sealed record AcknowledgeReleaseNotesCommand(List<Guid> Ids) : ICommand<ReleaseNoteResult>;
public sealed class AcknowledgeReleaseNotesCommandHandler(ReleaseNoteAccess access, IReleaseNoteRepository repository) : ICommandHandler<AcknowledgeReleaseNotesCommand, ReleaseNoteResult>
{
    public async Task<ReleaseNoteResult> HandleAsync(AcknowledgeReleaseNotesCommand command, CancellationToken ct)
    {
        if (command.Ids is null || command.Ids.Count is < 1 or > 500 || command.Ids.Contains(Guid.Empty)) return new(400, "شناسه انتشارها معتبر نیست.");
        var reader = await access.ReaderAsync(ct);
        return ReleaseNoteAdministration.Map(await repository.AcknowledgeAsync(reader, command.Ids.Distinct().ToArray(), DateTime.UtcNow, ct));
    }
}
