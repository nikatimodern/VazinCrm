using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Orders.Services;

namespace CRM.Core.ApplicationServices.Orders.Commands.SaveOrderHeads;
public sealed record SaveOrderHeadsCommand(List<OrderHead> Heads) : ICommand<int>;
public sealed class SaveOrderHeadsCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<SaveOrderHeadsCommand, int>
{
    public async Task<int> HandleAsync(SaveOrderHeadsCommand request, CancellationToken ct)
    {
        if (request.Heads == null || request.Heads.Count == 0)
            return 400;
        var current = await actor.GetAsync(ct);
        await repository.BeginAsync(ct);
        foreach (var head in request.Heads)
        {
            var old = head.Id == Guid.Empty ? null : await repository.GetHeadAsync(head.Id, false, ct);
            var create = old == null;
            if (!create && !OrderAccess.CanEdit(current, old!))
                return 204;
            if (!create && (head.Orders ?? []).Any(i => i.Id != Guid.Empty && !old!.Orders.Any(o => o.Id == i.Id)))
                return 400;
            head.OrderDateMildi = dates.ParsePersian(head.OrderDate);
            head.Deal = null !;
            head.SupplyFactorOrderHeads = null !;
            head.RequestAmountOrderHeads = null !;
            head.CashOrderHeads = null !;
            if (create)
            {
                head.UserCreator = current.User.FullName;
                head.UserCreatorID = current.User.Id;
            }
            else
            {
                head.CreateDate = old!.CreateDate;
                head.CreateDateTxt = old.CreateDateTxt;
                head.UserCreatorID = old.UserCreatorID;
                head.UserCreator = old.UserCreator;
                head.OrderNumber = old.OrderNumber;
            }

            foreach (var item in head.Orders ?? [])
                item.OrderHead = null !;
            await repository.SaveHeadAsync(head, create, ct);
            await repository.SaveChangesAsync(ct);
            await audit.LogAsync(create ? head : old!, create ? null : head, create ? LogActionEnum.ثبت : LogActionEnum.ویرایش, current.User, ct, create ? null : ["Orders"]);
        }

        if (request.Heads[0].DealId is Guid deal)
            await repository.TouchDealAsync(deal, DateTime.Now, dates.PersianNow(), ct);
        await repository.SaveChangesAsync(ct);
        await repository.CommitAsync(ct);
        return 200;
    }
}
