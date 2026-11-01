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

namespace CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderItem;
public sealed record UpdateOrderItemCommand(Order Item) : ICommand<int>;
public sealed class UpdateOrderItemCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<UpdateOrderItemCommand, int>
{
    public async Task<int> HandleAsync(UpdateOrderItemCommand request, CancellationToken ct)
    {
        var old = await repository.GetItemAsync(request.Item.Id, false, ct);
        if (old == null)
            return 404;
        var current = await actor.GetAsync(ct);
        var item = request.Item;
        item.OrderHead = null !;
        OrderAccess.Stamp(item, current, dates.PersianNow());
        await repository.SaveItemAsync(item, false, ct);
        await audit.LogAsync(old, item, LogActionEnum.ویرایش, current.User, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
