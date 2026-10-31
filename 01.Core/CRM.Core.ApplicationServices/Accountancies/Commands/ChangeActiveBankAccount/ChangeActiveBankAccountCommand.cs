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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.ChangeActiveBankAccount;
public sealed record ChangeActiveBankAccountCommand(BankAccount Model) : ICommand<AccountancyOutcome>;
public sealed class ChangeActiveBankAccountCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<ChangeActiveBankAccountCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(ChangeActiveBankAccountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var oldBa = await repository.GetBankAsync(model.Id, false, ct);
            var ba = await repository.GetBankAsync(model.Id, true, ct);
            if (ba == null)
                return new(404, "رکوردی یافت نشد!");
            ba.Active = model.Active;
            ba.UserModify = user.FullName;
            ba.UserModifyID = user.Id;
            ba.ModifyDate = DateTime.Now;
            ba.ModifyDateTxt = dates.PersianNow();
            await audit.LogAsync(oldBa, ba, LogActionEnum.تغییر_وضعیت, user, ct);
            await repository.SaveAsync(ct);
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
