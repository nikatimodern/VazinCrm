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

namespace CRM.Core.ApplicationServices.Cashes.Commands.ChangeStatusAllCashToPending;
public sealed record ChangeStatusAllCashToPendingCommand() : ICommand<CashOutcome>;
public sealed class ChangeStatusAllCashToPendingCommandHandler(ICashRepository repository, CurrentActor actor, CashWorkflow workflow) : ICommandHandler<ChangeStatusAllCashToPendingCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(ChangeStatusAllCashToPendingCommand request, CancellationToken ct)
    {
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var cashes = await repository.GetDraftsAsync(user.Id, ct);
            if (cashes.Count == 0)
                return new(400, "هیچ تنخواه آماده ارسالی یافت نشد.");
            await workflow.AttachCashesToNewGroup(cashes, CashStatusEnum.در_دست_تایید, user, ct);
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
