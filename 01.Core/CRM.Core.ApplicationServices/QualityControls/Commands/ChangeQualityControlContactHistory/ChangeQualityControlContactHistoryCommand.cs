using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.ChangeQualityControlContactHistory;

public sealed record ChangeQualityControlContactHistoryCommand(
    ChangeQualityControlContactHistoryRequest Request)
    : ICommand<ChangeQualityControlContactHistoryResult>;

public sealed class ChangeQualityControlContactHistoryCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IDocumentReadAccess permissions, IApplicationDates dates)
    : ICommandHandler<ChangeQualityControlContactHistoryCommand, ChangeQualityControlContactHistoryResult>
{
    public async Task<ChangeQualityControlContactHistoryResult> HandleAsync(
        ChangeQualityControlContactHistoryCommand command, CancellationToken ct)
    {
        var user = await actor.GetAsync(ct);
        if (!permissions.IsEnforced("QualityControl") && !user.HasRole(AppRoles.Admin))
            return new(403, "فقط مدیر ارشد امکان حذف یا بازیابی تاریخچه تماس را دارد.");
        return await repository.ChangeContactHistoryDeletionAsync(command.Request,
            await QualityControlAccessPolicy.ForAsync(user, permissions, "ManageContactHistory", ct), dates.PersianNow(), ct);
    }
}
