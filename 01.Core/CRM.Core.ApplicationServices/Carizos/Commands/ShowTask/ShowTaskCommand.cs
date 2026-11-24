using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Commands.ShowTask;
public sealed record ShowTaskCommand(Guid Id) : ICommand<int>;
public sealed class ShowTaskCommandHandler(ICarizoRepository repository, ICarizoEffects effects) : ICommandHandler<ShowTaskCommand, int>
{
    public async Task<int> HandleAsync(ShowTaskCommand request, CancellationToken ct)
    {
        var id = request.Id;
        if (!await repository.CanAccessAsync(id, "ChangeStatus", ct)) return 403;
        var data = await repository.GetAsync(id, true, ct);
        if (data == null) return 404;
        data.Remind = false;
        data.RemindDismiss = true;
        await repository.SaveAsync(ct);
        await effects.RefreshAsync(ct, id);
        return 200;
    }
}
