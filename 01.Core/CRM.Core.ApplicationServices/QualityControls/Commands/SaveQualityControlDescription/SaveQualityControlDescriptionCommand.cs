using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControlDescription;

public sealed record SaveQualityControlDescriptionCommand(SaveQualityControlDescriptionRequest Request) : ICommand<QualityControlSaveResult>;

public sealed class SaveQualityControlDescriptionCommandHandler(IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<SaveQualityControlDescriptionCommand, QualityControlSaveResult>
{
    public async Task<QualityControlSaveResult> HandleAsync(SaveQualityControlDescriptionCommand command, CancellationToken ct)
    {
        var input = command.Request;
        if (input.DealId == Guid.Empty || (input.RowVersion is not null && input.RowVersion.Length != 8))
            return new(400, "اطلاعات سفارش معتبر نیست.");
        if (input.Description?.Length > 4000)
            return new(400, "توضیحات نباید بیشتر از ۴۰۰۰ نویسه باشد.");

        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        return await repository.SaveDescriptionAsync(input, access, dates.PersianNow(), ct);
    }
}
