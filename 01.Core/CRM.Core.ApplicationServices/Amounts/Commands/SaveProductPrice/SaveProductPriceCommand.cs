using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;

namespace CRM.Core.ApplicationServices.Amounts.Commands.SaveProductPrice;
public sealed record SaveProductPriceCommand(ProductAmount Model) : ICommand<int>;
public sealed class SaveProductPriceCommandHandler(IProductPriceRepository repository, CurrentActor actor, IDocumentReadAccess permissions, IApplicationAudit audit, IApplicationDates dates) : ICommandHandler<SaveProductPriceCommand, int>
{
    public async Task<int> HandleAsync(SaveProductPriceCommand request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        if (!permissions.IsEnforced("ProductAmount") &&
            !current.HasRole("مدیر ارشد", "مدیر فروش", "مدیر تامین", "کارشناس تامین"))
            return 403;
        var user = current.User;
        var model = request.Model;
        var create = model.Id == Guid.Empty;
        if (!await permissions.HasAsync("Product", "View", ct) ||
            !await permissions.HasAsync("ProductAmount", "View", ct) ||
            !await permissions.HasAsync("ProductAmount", create ? "Create" : "Edit", ct))
            return 403;
        if (model.ProductId is not Guid productId || productId == Guid.Empty ||
            !double.IsFinite(model.From) || !double.IsFinite(model.To) || model.From < 0 || model.To < model.From ||
            model.RasmiAmount < 0 || model.NonRasmiAmount < 0)
            return 400;
        if (!await repository.ProductExistsAsync(productId, ct))
            return 404;
        var original = create ? null : await repository.GetAsync(model.Id, false, ct);
        if (!create && original == null)
            return 404;
        if (original != null && original.ProductId != model.ProductId)
            return 400;

        // Bind only the editable business fields; never accept caller-supplied audit or deletion state.
        model = new ProductAmount
        {
            Id = model.Id, ProductId = productId, From = model.From, To = model.To,
            Unit = model.Unit, UnitTxt = model.UnitTxt,
            RasmiAmount = model.RasmiAmount, NonRasmiAmount = model.NonRasmiAmount
        };
        if (create)
        {
            model.CreateDate = DateTime.Now;
            model.CreateDateTxt = dates.PersianNow();
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
