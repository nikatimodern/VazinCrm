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

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeDocTypeRequestAmount;
public sealed record ChangeDocTypeRequestAmountCommand(RequestAmount Model) : ICommand<RequestAmountOutcome>;
public sealed class ChangeDocTypeRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<ChangeDocTypeRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(ChangeDocTypeRequestAmountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var oldItem = await repository.GetAsync(model.Id, false, ct);
            var item = await repository.GetAsync(model.Id, true, ct);
            if (item == null)
                return new(404, "رکوردی یافت نشد!");
            item.DocType = model.DocType;
            item.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dates.PersianNow();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            await audit.LogAsync(oldItem, item, LogActionEnum.ویرایش, user, ct, ["RequestAmountSupplyFactor"]);
            await repository.SaveAsync(ct);
            await repository.CommitAsync(ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await repository.RollbackAsync(CancellationToken.None);
            return new(400);
        }
    }
}
