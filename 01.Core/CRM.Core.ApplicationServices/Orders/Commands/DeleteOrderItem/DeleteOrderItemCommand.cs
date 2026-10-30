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

namespace CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderItem;
public sealed record DeleteOrderItemCommand(Guid Id) : ICommand<int>;
public sealed class DeleteOrderItemCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<DeleteOrderItemCommand, int>
{
    public async Task<int> HandleAsync(DeleteOrderItemCommand request, CancellationToken ct)
    {
        var item = await repository.GetItemAsync(request.Id, true, ct);
        if (item == null)
            return 200;
        var current = await actor.GetAsync(ct);
        await audit.LogAsync(item, (Order? )null, LogActionEnum.حذف, current.User, ct);
        item.IsDeleted = true;
        OrderAccess.Stamp(item, current, dates.PersianNow());
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
