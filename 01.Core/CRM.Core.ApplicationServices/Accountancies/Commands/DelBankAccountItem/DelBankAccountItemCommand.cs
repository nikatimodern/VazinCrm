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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.DelBankAccountItem;
public sealed record DelBankAccountItemCommand(Guid Id) : ICommand<AccountancyOutcome>;
public sealed class DelBankAccountItemCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<DelBankAccountItemCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(DelBankAccountItemCommand request, CancellationToken ct)
    {
        var id = request.Id;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var bankAcc = await repository.GetBankAsync(id, true, ct);
            if (bankAcc == null)
                return new(404, "رکوردی یافت نشد!");
            await audit.LogAsync(bankAcc, null, LogActionEnum.حذف, user, ct);
            bankAcc.IsDeleted = true;
            bankAcc.UserModify = user.FullName;
            bankAcc.UserModifyID = user.Id;
            bankAcc.ModifyDate = DateTime.Now;
            bankAcc.ModifyDateTxt = dates.PersianNow();
            await repository.SaveAsync(ct);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(400);
        }

        return new(200);
    }
}
