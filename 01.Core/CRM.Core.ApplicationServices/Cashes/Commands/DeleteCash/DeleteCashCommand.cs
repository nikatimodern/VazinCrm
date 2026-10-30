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

namespace CRM.Core.ApplicationServices.Cashes.Commands.DeleteCash;
public sealed record DeleteCashCommand(Guid Id) : ICommand<CashOutcome>;
public sealed class DeleteCashCommandHandler(ICashRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<DeleteCashCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(DeleteCashCommand request, CancellationToken ct)
    {
        var id = request.Id;
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var item = await repository.GetAsync(id, true, ct);
            if (item == null)
                return new(404, "رکوردی یافت نشد!");
            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dates.PersianNow();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            await audit.LogAsync(item, null, LogActionEnum.حذف, user, ct);
            var acc = await repository.GetDebitAsync(id, ct);
            if (acc != null)
            {
                repository.RemoveDebit(acc);
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
