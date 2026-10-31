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

namespace CRM.Core.ApplicationServices.Carizos.Commands.TaskToggle;
public sealed record TaskToggleCommand(Guid Id, int BtnC) : ICommand<int>;
public sealed class TaskToggleCommandHandler(ICarizoRepository repository, ICarizoEffects effects) : ICommandHandler<TaskToggleCommand, int>
{
    public async Task<int> HandleAsync(TaskToggleCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var btnC = request.BtnC;
        var data = await repository.GetAsync(id, true, ct);
        var rem = data.Remind;
        var dismis = data.RemindDismiss;
        if (dismis == true && rem == false && btnC == 1)
        {
            data.RemindDismiss = true;
            data.Remind = true;
        }
        else if (dismis == true && rem == false && btnC == 2)
        {
            data.RemindDismiss = false;
            data.Remind = true;
        }
        else if (dismis == true && rem == true && btnC == 1)
        {
            data.RemindDismiss = true;
            data.Remind = false;
        }
        else if (dismis == true && rem == true && btnC == 2)
        {
            data.RemindDismiss = false;
            data.Remind = true;
        }
        else if (dismis == false && rem == true && btnC == 1)
        {
            data.RemindDismiss = true;
            data.Remind = false;
        }
        else if (dismis == false && rem == true && btnC == 2)
        {
            data.RemindDismiss = true;
            data.Remind = true;
        }
        else if (dismis == false && rem == false && btnC == 1)
        {
            data.RemindDismiss = false;
            data.Remind = true;
        }
        else if (dismis == false && rem == false && btnC == 2)
        {
            data.RemindDismiss = true;
            data.Remind = false;
        }

        await repository.SaveAsync(ct);
        await effects.RefreshAsync(ct, id);
        return 200;
    }
}
