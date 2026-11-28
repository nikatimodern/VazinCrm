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
public sealed class ChangeStatusAllAccCommandHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationAudit audit,
    IDepositFinancialRepository financial) : ICommandHandler<ChangeStatusAllAccCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(ChangeStatusAllAccCommand request, CancellationToken ct)
    {
        var pfId = request.PfId;
        var user = (await actor.GetAsync(ct)).User;
        var candidates = await repository.GetPendingAsync(pfId, ct);
        var locks = new List<IAsyncDisposable>();
        try
        {
            foreach (var chequeId in candidates.Where(acc => acc.PayType == PayTypeEnum.چکی).Select(acc => acc.Id).Order())
                locks.Add(await financial.LockAsync(chequeId, ct));

            var pending = new List<(Accountancy Current, Accountancy Original)>();
            foreach (var acc in candidates)
            {
                var original = await repository.GetAsync(acc.Id, false, false, ct);
                if (original == null || original.Status is not (AccStatusEnum.ثبت_اولیه or AccStatusEnum.رد_شده))
                    continue;
                if (original.GetChequeModificationRestriction() is { } restriction)
                    return new(409, restriction);
                pending.Add((acc, original));
            }

            foreach (var (acc, original) in pending)
            {
                acc.Status = AccStatusEnum.در_دست_تایید;
                await audit.LogAsync(original, acc, LogActionEnum.تغییر_وضعیت, user, ct, null, "ارسال به مالی");
            }
            await repository.SaveAsync(ct);
            return new(200);
        }
        finally
        {
            for (var index = locks.Count - 1; index >= 0; index--)
                await locks[index].DisposeAsync();
        }
    }
}
