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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.AddBankAccount;
public sealed record AddBankAccountCommand(BankAccount Model) : ICommand<AccountancyOutcome>;
public sealed class AddBankAccountCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationAudit audit) : ICommandHandler<AddBankAccountCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(AddBankAccountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.BankAccountTypeTxt = model.BankAccountType.ToString().Replace("_", " ");
            repository.SaveBank(model, true);
            await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
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
