using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;

namespace CRM.Core.ApplicationServices.Transfers.Commands.ChangeTransferStatus;
public sealed record ChangeTransferStatusCommand(Guid Id, TransferStatusEnum Status) : ICommand<int>;
public sealed class ChangeTransferStatusCommandHandler(ITransferRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates, ITransferEffects effects) : ICommandHandler<ChangeTransferStatusCommand, int>
{
    public async Task<int> HandleAsync(ChangeTransferStatusCommand request, CancellationToken ct)
    {
        var current = await actor.FindAsync(ct);
        if (current == null)
            return 401;
        var user = current.User;
        var original = await repository.GetAsync(request.Id, false, ct);
        var item = await repository.GetAsync(request.Id, true, ct);
        if (item == null)
            return 404;
        if (item.Status == request.Status)
            return 200;
        item.Status = request.Status;
        item.ModifyDate = DateTime.Now;
        item.ModifyDateTxt = dates.PersianNow();
        item.UserModify = user.FullName;
        item.UserModifyID = user.Id;
        await audit.LogAsync(original, item, LogActionEnum.تغییر_وضعیت, user, ct);
        await repository.SaveChangesAsync(ct);
        await effects.NotifyPendingAsync(ct);
        return 200;
    }
}
