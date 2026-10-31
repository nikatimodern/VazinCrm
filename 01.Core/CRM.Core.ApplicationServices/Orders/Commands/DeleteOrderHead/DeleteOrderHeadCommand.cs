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

namespace CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderHead;
public sealed record DeleteOrderHeadCommand(Guid Id) : ICommand<int>;
public sealed class DeleteOrderHeadCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit) : ICommandHandler<DeleteOrderHeadCommand, int>
{
    public async Task<int> HandleAsync(DeleteOrderHeadCommand request, CancellationToken ct)
    {
        var head = await repository.GetHeadAsync(request.Id, true, ct);
        if (head == null)
            return 404;
        var current = await actor.GetAsync(ct);
        if (!OrderAccess.CanEdit(current, head))
            return 204;
        await audit.LogAsync(head, (OrderHead? )null, LogActionEnum.حذف, current.User, ct);
        head.IsDeleted = true;
        OrderAccess.Stamp(head, current, dates.PersianNow());
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
