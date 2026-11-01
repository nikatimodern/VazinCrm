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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.DelAccountancy;
public sealed record DelAccountancyCommand(Guid Id) : ICommand<AccountancyOutcome>;
public sealed class DelAccountancyCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, IAccountancyEffects effects) : ICommandHandler<DelAccountancyCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(DelAccountancyCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var user = (await actor.GetAsync(ct)).User;
        var acc = await repository.GetAsync(id, false, true, ct);
        if (acc == null)
            return new(404, "رکوردی یافت نشد!");
        await audit.LogAsync(acc, null, LogActionEnum.حذف, user, ct);
        acc.IsDeleted = true;
        acc.UserModify = user.FullName;
        acc.UserModifyID = user.Id;
        acc.ModifyDate = DateTime.Now;
        acc.ModifyDateTxt = dates.PersianNow();
        await repository.SaveAsync(ct);
        await effects.RefreshTasksAsync(ct);
        await repository.RemoveTasksAsync(id, ct);
        await repository.SaveAsync(ct);
        return new(200);
    }
}
