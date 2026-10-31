using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;

namespace CRM.Core.ApplicationServices.Amounts.Commands.DeleteProductPrice;
public sealed record DeleteProductPriceCommand(Guid Id) : ICommand<int>;
public sealed class DeleteProductPriceCommandHandler(IProductPriceRepository repository, CurrentActor actor, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<DeleteProductPriceCommand, int>
{
    public async Task<int> HandleAsync(DeleteProductPriceCommand request, CancellationToken ct)
    {
        var user = (await actor.GetAsync(ct)).User;
        var price = await repository.GetAsync(request.Id, true, ct);
        if (price == null)
            return 404;
        await audit.LogAsync(price, null, LogActionEnum.حذف, user, ct);
        price.IsDeleted = true;
        price.ModifyDate = DateTime.Now;
        price.ModifyDateTxt = dates.PersianNow();
        price.UserModify = user.FullName;
        price.UserModifyID = user.Id;
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
