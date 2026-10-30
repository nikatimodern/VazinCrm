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

namespace CRM.Core.ApplicationServices.Cashes.Commands.ApproveCashGroup;
public sealed record ApproveCashGroupCommand(Guid GroupId) : ICommand<CashOutcome>;
public sealed class ApproveCashGroupCommandHandler(ICashRepository repository, CurrentActor actor, CashWorkflow workflow) : ICommandHandler<ApproveCashGroupCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(ApproveCashGroupCommand request, CancellationToken ct)
    {
        var groupId = request.GroupId;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var cashes = await repository.GetGroupItemsAsync(groupId, false, ct);
            if (cashes.Count == 0)
                return new(404, "گروه یافت نشد یا تنخواهی در آن وجود ندارد.");
            foreach (var cash in cashes)
            {
                await workflow.ChangeCashStatus(CashStatusEnum.تایید_شده, cash, user, ct);
            }

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
