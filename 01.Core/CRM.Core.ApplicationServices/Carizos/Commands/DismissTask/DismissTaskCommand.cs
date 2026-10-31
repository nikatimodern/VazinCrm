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

namespace CRM.Core.ApplicationServices.Carizos.Commands.DismissTask;
public sealed record DismissTaskCommand(Guid Id) : ICommand<int>;
public sealed class DismissTaskCommandHandler(ICarizoRepository repository, ICarizoEffects effects) : ICommandHandler<DismissTaskCommand, int>
{
    public async Task<int> HandleAsync(DismissTaskCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var data = await repository.GetAsync(id, true, ct);
        if (data != null)
        {
            data.RemindDismiss = true;
            await repository.SaveAsync(ct);
            await effects.RefreshAsync(ct, id);
        }

        return 200;
    }
}
