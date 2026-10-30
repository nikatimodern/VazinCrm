using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;

namespace CRM.Core.ApplicationServices.Transfers.Commands.SaveTransfer;
public sealed record SaveTransferCommand(Transfer Model) : ICommand<int>;
public sealed class SaveTransferCommandHandler(ITransferRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates, ITransferEffects effects) : ICommandHandler<SaveTransferCommand, int>
{
    public async Task<int> HandleAsync(SaveTransferCommand request, CancellationToken ct)
    {
        await repository.BeginAsync(ct);
        var user = (await actor.GetAsync(ct)).User;
        var model = request.Model;
        var create = model.Id == Guid.Empty;
        var original = create ? null : await repository.GetAsync(model.Id, false, ct);
        if (!create && original == null)
            return 404;
        if (create)
        {
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
        }
        else
        {
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = dates.PersianNow();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;
        }

        model.TransferDateMiladi = dates.ParsePersian(model.TransferDate) ?? DateTime.Now;
        await repository.SaveAsync(model, create, ct);
        await repository.SaveChangesAsync(ct);
        await audit.LogAsync(original ?? model, create ? null : model, create ? LogActionEnum.ثبت : LogActionEnum.ویرایش, user, ct);
        var attachments = effects.SaveAttachments(model.Attachments ?? []);
        foreach (var attachment in attachments ?? [])
            attachment.EntityId = model.Id;
        repository.SaveAttachments(attachments ?? []);
        await repository.SaveChangesAsync(ct);
        await repository.CommitAsync(ct);
        return 200;
    }
}
