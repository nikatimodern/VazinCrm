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

namespace CRM.Core.ApplicationServices.Carizos.Commands.UnCheckTask;
public sealed record UnCheckTaskCommand(Guid Id) : ICommand<object>;
public sealed class UnCheckTaskCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationAudit audit, ICarizoEffects effects) : ICommandHandler<UnCheckTaskCommand, object>
{
    public async Task<object> HandleAsync(UnCheckTaskCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var user = (await actor.GetAsync(ct)).User;
        var oldTa = await repository.GetAsync(id, false, ct);
        var Ta = await repository.GetAsync(id, true, ct);
        if (Ta.TaskType == TaskTypeEnum.تکی)
        {
            Ta.TaskStatus = false;
        }
        else if (Ta.TaskType == TaskTypeEnum.اختصاصی)
        {
            if (Ta.UserCreatorID == user.Id)
            {
                Ta.TaskStatus = false;
            }
            else
            {
                var MasterTask = await repository.GetAsync(Ta.ShareTaskId, true, ct);
                if (!MasterTask.TaskStatus)
                {
                    Ta.TaskStatus = false;
                    MasterTask.TaskUsers.Where(f => f.UserId == Guid.Parse(Ta.AssignUserId)).FirstOrDefault().completed = false;
                }
                else
                {
                    return new
                    {
                        Status = 201,
                        error = "این تسک توسط سازنده پایان یافته است",
                        data = ""
                    };
                }
            }
        }
        else //TaskTypeEnum.اشتراکی
        {
            var AllTask = await repository.GetSharedAsync(Ta.ShareTaskId, id, ct);
            foreach (var item in AllTask)
            {
                item.TaskStatus = false;
                item.CompleteTaskUser = null;
                item.CompleteTaskUserId = null;
                if (item.TaskUsers.Any()) //کسی که تسک یوزر داره پرند هست و سازنده می باشد
                {
                    item.TaskUsers.Where(f => f.UserId == Guid.Parse(user.Id)).FirstOrDefault().completed = false;
                }
            }

            Ta.TaskStatus = false;
            Ta.CompleteTaskUser = null;
            Ta.CompleteTaskUserId = null;
        }

        await audit.LogAsync(oldTa, Ta, LogActionEnum.ویرایش, user, ct);
        await repository.SaveAsync(ct);
        await effects.RefreshAsync(ct, id);
        return new
        {
            Status = 200,
            error = "",
            data = ""
        };
        ;
    }
}
