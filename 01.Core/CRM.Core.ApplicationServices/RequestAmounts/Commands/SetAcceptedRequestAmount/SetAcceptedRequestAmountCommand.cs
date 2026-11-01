using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.SetAcceptedRequestAmount;
public sealed record SetAcceptedRequestAmountCommand(RequestAmount Model) : ICommand<RequestAmountOutcome>;
public sealed class SetAcceptedRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, IRequestAmountEffects effects) : ICommandHandler<SetAcceptedRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(SetAcceptedRequestAmountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var oldItem = await repository.GetAsync(model.Id, false, ct);
            var item = await repository.GetAsync(model.Id, true, ct);
            if (item == null)
                return new(404, "رکوردی یافت نشد!");
            item.AcceptedAmount = model.AcceptedAmount;
            item.RequestStatus = RequestStatusEnum.در_انتظار_ارسال_به_مالی;
            item.RequestStatusTxt = nameof(RequestStatusEnum.در_انتظار_ارسال_به_مالی).Replace("_", " ");
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dates.PersianNow();
            item.AcceptedAmountPercent = model.AcceptedAmountPercent;
            await audit.LogAsync(oldItem, item, LogActionEnum.تایید_و_تغییر_وضعیت, user, ct);
            await repository.SaveAsync(ct);
            await effects.NotifyAsync(["مدیر عامل", "مدیر ارشد"], ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(400);
        }
    }
}
