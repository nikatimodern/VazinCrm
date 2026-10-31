using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Commands.DeleteCheckbook;
public sealed record DeleteCheckbookCommand(Guid Id) : ICommand<int>;
public sealed class DeleteCheckbookCommandHandler(ICheckRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<DeleteCheckbookCommand, int>
{
    public async Task<int> HandleAsync(DeleteCheckbookCommand request, CancellationToken ct)
    {
        var user = (await actor.GetAsync(ct)).User;
        var item = await repository.GetCheckbookAsync(request.Id, ct);
        if (item == null)
            return 404;
        item.IsDeleted = true;
        item.ModifyDate = DateTime.Now;
        item.ModifyDateTxt = dates.PersianNow();
        item.UserModify = user.FullName;
        item.UserModifyID = user.Id;
        foreach (var check in item.Checks ?? [])
            check.IsDeleted = true;
        await audit.LogAsync(item, null, LogActionEnum.حذف, user, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
