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

namespace CRM.Core.ApplicationServices.Carizos.Commands.DelTask;
public sealed record DelTaskCommand(Guid Id) : ICommand<int>;
public sealed class DelTaskCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationAudit audit, ICarizoEffects effects) : ICommandHandler<DelTaskCommand, int>
{
    public async Task<int> HandleAsync(DelTaskCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var user = (await actor.GetAsync(ct)).User;
        var data = await repository.GetAsync(id, true, ct);
        await audit.LogAsync(data, null, LogActionEnum.حذف, user, ct);
        if (data.TaskType != TaskTypeEnum.تکی && data.UserCreatorID == user.Id)
        {
            var alltask = await repository.GetSharedAsync(data.Id, null, ct);
            foreach (var item in alltask)
            {
                item.IsDeleted = true;
            }

            data.IsDeleted = true;
        }
        else
            data.IsDeleted = true;
        await repository.SaveAsync(ct);
        await effects.RefreshAsync(ct, id);
        return 200;
    }
}
