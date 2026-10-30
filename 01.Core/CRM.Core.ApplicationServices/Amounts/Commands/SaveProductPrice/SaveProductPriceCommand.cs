using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;

namespace CRM.Core.ApplicationServices.Amounts.Commands.SaveProductPrice;
public sealed record SaveProductPriceCommand(ProductAmount Model) : ICommand<int>;
public sealed class SaveProductPriceCommandHandler(IProductPriceRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<SaveProductPriceCommand, int>
{
    public async Task<int> HandleAsync(SaveProductPriceCommand request, CancellationToken ct)
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

        await repository.SaveAsync(model, create, ct);
        await audit.LogAsync(original ?? model, create ? null : model, create ? LogActionEnum.ثبت : LogActionEnum.ویرایش, user, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
