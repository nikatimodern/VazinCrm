using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Commands.SaveCheck;
public sealed record SaveCheckCommand(Check Model) : ICommand<int>;
public sealed class SaveCheckCommandHandler(ICheckRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<SaveCheckCommand, int>
{
    public async Task<int> HandleAsync(SaveCheckCommand request, CancellationToken ct)
    {
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

        model.DueDateMiladi = dates.ParsePersian(model.DueDate);
        await repository.SaveCheckAsync(model, create, ct);
        await audit.LogAsync(original ?? model, create ? null : model, create ? LogActionEnum.ثبت : LogActionEnum.ویرایش, user, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
