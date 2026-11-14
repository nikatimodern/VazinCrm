using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.MarkQualityControlInvalidNumber;

public sealed record MarkQualityControlInvalidNumberCommand(MarkQualityControlInvalidNumberRequest Request) : ICommand<QualityControlSaveResult>;

public sealed class MarkQualityControlInvalidNumberCommandHandler(IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<MarkQualityControlInvalidNumberCommand, QualityControlSaveResult>
{
    public async Task<QualityControlSaveResult> HandleAsync(MarkQualityControlInvalidNumberCommand command, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        var request = command.Request;
        if (request.DealId == Guid.Empty || request.RowVersion is { Length: not 8 })
            return new(400, "اطلاعات کنترل کیفیت معتبر نیست. فهرست را به‌روز کنید.");
        return await repository.MarkInvalidNumberAsync(request, access, dates.PersianNow(), ct);
    }
}
