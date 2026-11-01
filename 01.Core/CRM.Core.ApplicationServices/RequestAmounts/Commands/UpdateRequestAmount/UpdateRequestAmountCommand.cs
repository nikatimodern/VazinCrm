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

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.UpdateRequestAmount;
public sealed record UpdateRequestAmountCommand(RequestAmount Model) : ICommand<RequestAmountOutcome>;
public sealed class UpdateRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<UpdateRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(UpdateRequestAmountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            model.RequestStatusTxt = model.RequestStatus.ToString().Replace("_", " ");
            model.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = dates.PersianNow();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;
            var entity = await repository.GetAsync(model.Id, true, ct);
            if (entity == null)
                return new(404, "رکوردی یافت نشد!");
            await audit.LogAsync(entity, model, LogActionEnum.ویرایش, user, ct, ["RequestAmountOrderHeads"]);
            repository.Apply(entity, model);
            entity.RequestAmountSupplyFactor = model.RequestAmountSupplyFactor;
            entity.RequestAmountOrderHeads = model.RequestAmountOrderHeads;
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
