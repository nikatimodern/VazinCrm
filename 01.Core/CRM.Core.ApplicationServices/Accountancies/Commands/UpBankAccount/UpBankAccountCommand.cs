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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.UpBankAccount;
public sealed record UpBankAccountCommand(BankAccount Model) : ICommand<AccountancyOutcome>;
public sealed class UpBankAccountCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<UpBankAccountCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(UpBankAccountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var oldBa = await repository.GetBankAsync(model.Id, false, ct);
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = dates.PersianNow();
            repository.SaveBank(model, false);
            await audit.LogAsync(oldBa, model, LogActionEnum.ویرایش, user, ct);
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
