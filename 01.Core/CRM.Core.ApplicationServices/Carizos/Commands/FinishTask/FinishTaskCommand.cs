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

namespace CRM.Core.ApplicationServices.Carizos.Commands.FinishTask;
public sealed record FinishTaskCommand(Guid Id) : ICommand<object>;
public sealed class FinishTaskCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationAudit audit, ICarizoEffects effects) : ICommandHandler<FinishTaskCommand, object>
{
    public async Task<object> HandleAsync(FinishTaskCommand request, CancellationToken ct)
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
                !await repository.CanPropagateStatusAsync(parent.Id, user.Id, ct) ||
                (parent.UserCreatorID != user.Id && (parent.TaskUsers == null ||
                 !parent.TaskUsers.Any(member => !member.IsDeleted && member.UserId.ToString() == user.Id))))
                return 403;
        }
        if (Ta.TaskType == TaskTypeEnum.تکی)
        {
            Ta.TaskStatus = true;
            Ta.CompleteTaskUser = user.FullName;
            Ta.CompleteTaskUserId = user.Id;
        }
        else if (Ta.TaskType == TaskTypeEnum.اختصاصی)
        {
            var Parent = await repository.GetAsync(Ta.ShareTaskId, true, ct);
            if (Parent.UserCreatorID == user.Id)
            {
                if (Parent.TaskUsers.Where(f => f.completed == false).Any())
                    return new
                    {
                        Status = 201,
                        error = "کاربرانی در حال انجام این تسک هستند",
                        data = ""
                    };
                else
                {
                    Ta.TaskStatus = true;
                    Ta.CompleteTaskUser = user.FullName;
                    Ta.CompleteTaskUserId = user.Id;
                }
            }
            else
            {
                Parent.TaskUsers.Where(f => f.UserId == Guid.Parse(user.Id)).FirstOrDefault().completed = true;
                Ta.TaskStatus = true;
                Ta.CompleteTaskUser = user.FullName;
                Ta.CompleteTaskUserId = user.Id;
            }
        }
        else //TaskTypeEnum.اشتراکی
        {
            var AllTask = await repository.GetSharedAsync(Ta.ShareTaskId, id, ct);
            foreach (var item in AllTask)
            {
                item.TaskStatus = true;
                item.CompleteTaskUser = user.FullName;
                item.CompleteTaskUserId = user.Id;
                if (item.TaskUsers.Any()) //کسی که تسک یوزر داره پرند هست و سازنده می باشد
                {
                    var member = item.TaskUsers.FirstOrDefault(f => !f.IsDeleted && f.UserId.ToString() == user.Id);
                    if (member != null) member.completed = true;
                }
            }

            Ta.TaskStatus = true;
            Ta.CompleteTaskUser = user.FullName;
            Ta.CompleteTaskUserId = user.Id;
        }

        await audit.LogAsync(oldTa, Ta, LogActionEnum.ویرایش, user, ct);
        await repository.SaveAsync(ct);
        await effects.RefreshAsync(ct, id);
        return 200;
    }
}
