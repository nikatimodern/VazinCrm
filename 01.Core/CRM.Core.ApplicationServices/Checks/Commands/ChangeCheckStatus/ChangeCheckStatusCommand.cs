using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Commands.ChangeCheckStatus;
public sealed record ChangeCheckStatusCommand(Guid Id, CheckStatus Status) : ICommand<int>;
public sealed class ChangeCheckStatusCommandHandler(ICheckRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<ChangeCheckStatusCommand, int>
{
    public async Task<int> HandleAsync(ChangeCheckStatusCommand request, CancellationToken ct)
    {
        if (request.Id == Guid.Empty)
            return 400;
        var user = (await actor.GetAsync(ct)).User;
        var original = await repository.GetAsync(request.Id, false, ct);
        var item = await repository.GetAsync(request.Id, true, ct);
        if (item == null)
            return 404;
        item.Status = request.Status;
        item.ModifyDate = DateTime.Now;
        item.ModifyDateTxt = dates.PersianNow();
        item.UserModify = user.FullName;
        item.UserModifyID = user.Id;
        await audit.LogAsync(original, item, LogActionEnum.تغییر_وضعیت, user, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
