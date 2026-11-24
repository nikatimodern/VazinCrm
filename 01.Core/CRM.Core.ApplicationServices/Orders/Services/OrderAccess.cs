using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Framework.Domain;

namespace CRM.Core.ApplicationServices.Orders.Services;
public static class OrderAccess
{
    public static OrderReader Reader(ApplicationActor actor) => new(actor.User.Id, actor.HasRole("مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی"), actor.HasRole("کارشناس تامین"));
    public static async Task<bool> CanMutateAsync(IOrderRepository repository, ApplicationActor actor, OrderHead head, string action, CancellationToken ct) =>
        await repository.GetCatalogHeadAccessAsync(head.Id, action, ct) ?? CanEditLegacy(actor, head);
    private static bool CanEditLegacy(ApplicationActor actor, OrderHead head) => actor.User.Id == head.UserCreatorID || actor.User.Id == head.UserTaminId.ToString() || actor.HasRole("مدیر ارشد", "مدیر فروش", "مدیر تامین");
    public static void Stamp(BaseEntity entity, ApplicationActor actor, string date)
    {
        entity.UserModify = actor.User.FullName;
        entity.UserModifyID = actor.User.Id;
        entity.ModifyDate = DateTime.Now;
        entity.ModifyDateTxt = date;
    }
}
