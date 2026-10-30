using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;

namespace CRM.Core.ApplicationServices.Transfers.Commands.DeleteTransfer;
public sealed record DeleteTransferCommand(Guid Id) : ICommand<int>;
public sealed class DeleteTransferCommandHandler(ITransferRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<DeleteTransferCommand, int>
{
    public async Task<int> HandleAsync(DeleteTransferCommand request, CancellationToken ct)
    {
        var user = (await actor.GetAsync(ct)).User;
        var item = await repository.GetAsync(request.Id, true, ct);
        if (item == null)
            return 404;
        item.IsDeleted = true;
        item.ModifyDate = DateTime.Now;
        item.ModifyDateTxt = dates.PersianNow();
        item.UserModify = user.FullName;
        item.UserModifyID = user.Id;
        await audit.LogAsync(item, null, LogActionEnum.حذف, user, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
