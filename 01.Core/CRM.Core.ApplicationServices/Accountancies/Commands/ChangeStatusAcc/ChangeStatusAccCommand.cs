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

namespace CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAcc;
public sealed record ChangeStatusAccCommand(Guid Id) : ICommand<AccountancyOutcome>;
public sealed class ChangeStatusAccCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationAudit audit) : ICommandHandler<ChangeStatusAccCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(ChangeStatusAccCommand request, CancellationToken ct)
    {
        var id = request.Id;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var oldAcc = await repository.GetAsync(id, false, false, ct);
            var acc = await repository.GetAsync(id, false, true, ct);
            if (acc == null)
                return new(404, "رکوردی یافت نشد!");
            acc.Status = AccStatusEnum.در_دست_تایید;
            await audit.LogAsync(oldAcc, acc, LogActionEnum.تغییر_وضعیت, user, ct, null, "ارسال به مالی");
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
