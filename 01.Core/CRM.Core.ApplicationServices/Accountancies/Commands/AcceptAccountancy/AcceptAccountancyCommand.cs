using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.ApplicationServices.Accountancies.Services;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.AcceptAccountancy;
public sealed record AcceptAccountancyCommand(AccStatusEnum Status, Guid Id, string Description) : ICommand<AccountancyOutcome>;
public sealed class AcceptAccountancyCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, IAccountancyEffects effects) : ICommandHandler<AcceptAccountancyCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(AcceptAccountancyCommand request, CancellationToken ct)
    {
        var status = request.Status;
        var Id = request.Id;
        var Description = request.Description;
        var user = (await actor.GetAsync(ct)).User;
        var oldAcc = await repository.GetAsync(request.Id, false, false, ct);
        var acc = await repository.GetAsync(request.Id, false, true, ct);
        if (acc == null)
            return new(404, "رکوردی یافت نشد!");
        acc.CommentText = request.Description;
        if (acc.PayType is PayTypeEnum.نقدی or PayTypeEnum.تهاتری)
        {
            acc.Vosoled = status == AccStatusEnum.تایید_شده;
        }

        acc.UserModify = user.FullName;
        acc.UserModifyID = user.Id;
        acc.ModifyDate = DateTime.Now;
        acc.ModifyDateTxt = dates.PersianNow();
        acc.Status = status;
        if (status == AccStatusEnum.تایید_شده)
        {
            acc.AcceptedUser = user.FullName;
            acc.AcceptedUserId = user.Id;
        }
        else
        {
            acc.AcceptedUser = null;
            acc.AcceptedUserId = null;
        }

        var action = acc.Status switch
        {
            AccStatusEnum.تایید_شده => LogActionEnum.تایید_و_تغییر_وضعیت,
            AccStatusEnum.رد_شده => LogActionEnum.رد_و_تغییر_وضعیت,
            _ => LogActionEnum.تغییر_وضعیت
        };
        await audit.LogAsync(oldAcc, acc, action, user, ct);
        if (acc.Status == AccStatusEnum.رد_شده)
            await effects.RejectNotificationAsync(acc, user, ct);
        if (acc.PayType is PayTypeEnum.اعتباری or PayTypeEnum.چکی or PayTypeEnum.تهاتری)
        {
            if (status == AccStatusEnum.تایید_شده)
            {
                await effects.SetTasksAsync(acc, true, ct);
            }
            else
            {
                await effects.SetTasksAsync(acc, false, ct);
            }
        }

        await repository.SaveAsync(ct);
        await effects.NotifyPendingAsync(ct);
        return new(200);
    }
}
