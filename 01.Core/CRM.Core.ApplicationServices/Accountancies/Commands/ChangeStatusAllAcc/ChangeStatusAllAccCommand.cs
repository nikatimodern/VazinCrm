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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAllAcc;
public sealed record ChangeStatusAllAccCommand(Guid PfId) : ICommand<AccountancyOutcome>;
public sealed class ChangeStatusAllAccCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationAudit audit) : ICommandHandler<ChangeStatusAllAccCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(ChangeStatusAllAccCommand request, CancellationToken ct)
    {
        var pfId = request.PfId;
        var user = (await actor.GetAsync(ct)).User;
        var accS = await repository.GetPendingAsync(pfId, ct);
        foreach (var acc in accS)
        {
            var oldAcc = await repository.GetAsync(acc.Id, false, false, ct);
            acc.Status = AccStatusEnum.در_دست_تایید;
            await audit.LogAsync(oldAcc, acc, LogActionEnum.تغییر_وضعیت, user, ct, null, "ارسال به مالی");
        }

        await repository.SaveAsync(ct);
        return new(200);
    }
}
