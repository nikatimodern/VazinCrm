using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.DeleteRequestAmount;
public sealed record DeleteRequestAmountCommand(Guid Id) : ICommand<RequestAmountOutcome>;
public sealed class DeleteRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<DeleteRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(DeleteRequestAmountCommand request, CancellationToken ct)
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
            foreach (var i in item.RequestAmountOrderHeads ?? [])
            {
                i.IsDeleted = true;
            }

            foreach (var i in item.RequestAmountSupplyFactor ?? [])
            {
                i.IsDeleted = true;
            }

            await audit.LogAsync(item, null, LogActionEnum.حذف, user, ct);
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
