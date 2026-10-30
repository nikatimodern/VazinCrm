using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Commands.AddBaseCash;
public sealed record AddBaseCashCommand(BaseCash Model) : ICommand<CashOutcome>;
public sealed class AddBaseCashCommandHandler(ICashRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<AddBaseCashCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(AddBaseCashCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        var isDuplicate = await repository.IsDuplicateBaseAsync(model, ct);
        if (isDuplicate)
            return new(400, "این عنوان هزینه قبلاً ثبت شده است.");
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            model.BaseCashTypeTxt = model.BaseCashType.ToString();
            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                repository.SaveBase(model, true);
                await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
            }
            else
            {
                var oldBaseCash = await repository.GetBaseAsync(model.Id, false, ct);
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dates.PersianNow();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                repository.SaveBase(model, false);
                await audit.LogAsync(oldBaseCash, model, LogActionEnum.ویرایش, user, ct);
            }

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
