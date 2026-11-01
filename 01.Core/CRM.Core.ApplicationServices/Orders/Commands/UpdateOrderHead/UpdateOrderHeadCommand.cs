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

namespace CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHead;
public sealed record UpdateOrderHeadCommand(OrderHead Head) : ICommand<int>;
public sealed class UpdateOrderHeadCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<UpdateOrderHeadCommand, int>
{
    public async Task<int> HandleAsync(UpdateOrderHeadCommand request, CancellationToken ct)
    {
        var old = await repository.GetHeadAsync(request.Head.Id, false, ct);
        if (old == null)
            return 204;
        var current = await actor.GetAsync(ct);
        if (!OrderAccess.CanEdit(current, old))
            return 204;
        if ((request.Head.Orders ?? []).Any(i => i.Id != Guid.Empty && !old.Orders.Any(o => o.Id == i.Id)))
            return 400;
        var head = await repository.GetHeadAsync(old.Id, false, ct);
        head!.Tel = request.Head.Tel;
        head.OrderDate = request.Head.OrderDate;
        head.Description = request.Head.Description;
        OrderAccess.Stamp(head, current, dates.PersianNow());
        var replacement = request.Head.Orders ?? [];
        foreach (var item in replacement)
        {
            item.OrderHead = null !;
            item.OrderHeadId = head.Id;
        }

        // Repository copies scalar item values into the tracked collection.
        head.Orders = replacement;
        await repository.SaveHeadAsync(head, false, ct);
        await audit.LogAsync(old, head, LogActionEnum.ویرایش, current.User, ct, ["Orders"]);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
