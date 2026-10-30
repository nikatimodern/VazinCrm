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

namespace CRM.Core.ApplicationServices.Cashes.Commands.ChangeStatusCash;
public sealed record ChangeStatusCashCommand(Guid Id, CashStatusEnum Status) : ICommand<CashOutcome>;
public sealed class ChangeStatusCashCommandHandler(ICashRepository repository, CurrentActor actor, CashWorkflow workflow) : ICommandHandler<ChangeStatusCashCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(ChangeStatusCashCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var status = request.Status;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var data = await repository.GetAsync(id, true, ct);
            if (data == null)
                return new(404, "تنخواه یافت نشد.");
            if (data.CashGroupId == null && data.Status == CashStatusEnum.ثبت_موقت && status == CashStatusEnum.در_دست_تایید)
            {
                await workflow.AttachCashesToNewGroup([data], status, user, ct);
            }
            else
            {
                await workflow.ChangeCashStatus(status, data, user, ct);
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
