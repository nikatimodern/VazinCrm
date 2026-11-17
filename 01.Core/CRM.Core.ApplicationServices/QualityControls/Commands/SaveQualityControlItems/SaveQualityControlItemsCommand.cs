using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControlItems;

public sealed record SaveQualityControlItemsCommand(SaveQualityControlItemsRequest Request)
    : ICommand<SaveQualityControlItemsResult>;

public sealed class SaveQualityControlItemsCommandHandler(
    IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<SaveQualityControlItemsCommand, SaveQualityControlItemsResult>
{
    public async Task<SaveQualityControlItemsResult> HandleAsync(SaveQualityControlItemsCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.CustomerId == Guid.Empty || request.Items == null || request.Items.Count is < 1 or > 500)
            return new(400, "مشتری یا اقلام کنترل کیفیت معتبر نیست.");
        return await repository.SaveItemsAsync(request,
            QualityControlAccessPolicy.For(await actor.GetAsync(ct)), dates.PersianNow(), ct);
    }
}
