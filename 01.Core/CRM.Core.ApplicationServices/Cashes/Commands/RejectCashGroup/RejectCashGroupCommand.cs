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

namespace CRM.Core.ApplicationServices.Cashes.Commands.RejectCashGroup;
public sealed record RejectCashGroupCommand(Guid GroupId) : ICommand<CashOutcome>;
public sealed class RejectCashGroupCommandHandler(ICashRepository repository, CurrentActor actor, CashWorkflow workflow) : ICommandHandler<RejectCashGroupCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(RejectCashGroupCommand request, CancellationToken ct)
    {
        var groupId = request.GroupId;
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var group = await repository.GetGroupAsync(groupId, ct);
            var cashes = await repository.GetGroupItemsAsync(groupId, false, ct);
            if (cashes.Count == 0 && group == null)
                return new(404, "گروه مورد نظر یافت نشد.");
            foreach (var cash in cashes)
            {
                await workflow.ChangeCashStatus(CashStatusEnum.ثبت_موقت, cash, user, ct);
                cash.CashGroupId = null;
            }

            if (group != null)
            {
                repository.RemoveGroup(group);
            }

            await repository.SaveAsync(ct);
            await repository.CommitAsync(ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await repository.RollbackAsync(CancellationToken.None);
            return new(400);
        }
    }
}
