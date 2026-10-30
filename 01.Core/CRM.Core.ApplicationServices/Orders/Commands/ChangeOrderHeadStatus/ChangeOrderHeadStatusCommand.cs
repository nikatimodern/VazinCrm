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
using CRM.Core.ApplicationServices.Orders.Abstractions;

namespace CRM.Core.ApplicationServices.Orders.Commands.ChangeOrderHeadStatus;
public sealed record ChangeOrderHeadStatusCommand(Guid Id, int Status, bool SendSms) : ICommand<int>;
public sealed class ChangeOrderHeadStatusCommandHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, IOrderEffects effects) : ICommandHandler<ChangeOrderHeadStatusCommand, int>
{
    public async Task<int> HandleAsync(ChangeOrderHeadStatusCommand request, CancellationToken ct)
    {
        var old = await repository.GetHeadAsync(request.Id, false, ct);
        if (old == null)
            return 404;
        if (!Enum.IsDefined(typeof(OrderHeadStatusEnum), request.Status))
            return 400;
        var current = await actor.GetAsync(ct);
        if (!OrderAccess.CanEdit(current, old))
            return 204;
        var head = await repository.GetHeadAsync(request.Id, true, ct);
        head!.Status = (OrderHeadStatusEnum)request.Status;
        OrderAccess.Stamp(head, current, dates.PersianNow());
        await audit.LogAsync(old, head, LogActionEnum.تغییر_وضعیت, current.User, ct);
        await repository.SaveChangesAsync(ct);
        if (request.Status == 3 && request.SendSms)
            await effects.QueueReadyMessageAsync(await repository.GetCustomerMobileAsync(head, ct) ?? "", head.PrefactorSer.ToString() ?? "", ct);
        return 200;
    }
}
