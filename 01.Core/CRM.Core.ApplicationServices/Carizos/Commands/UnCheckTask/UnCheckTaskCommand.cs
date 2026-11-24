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
        if (!await repository.CanAccessAsync(id, "ChangeStatus", ct)) return 403;
        var user = (await actor.GetAsync(ct)).User;
        var oldTa = await repository.GetAsync(id, false, ct);
        var Ta = await repository.GetAsync(id, true, ct);
        if (Ta == null) return 404;
        if (Ta.TaskType != TaskTypeEnum.تکی)
        {
            var parent = await repository.GetAsync(Ta.ShareTaskId, false, ct);
            if (parent == null || parent.ShareTaskId != parent.Id ||
                (parent.UserCreatorID != user.Id && (parent.TaskUsers == null ||
                 !parent.TaskUsers.Any(member => !member.IsDeleted && member.UserId.ToString() == user.Id))))
                return 403;
        }
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
                    var member = item.TaskUsers.FirstOrDefault(f => !f.IsDeleted && f.UserId.ToString() == user.Id);
                    if (member != null) member.completed = false;
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
