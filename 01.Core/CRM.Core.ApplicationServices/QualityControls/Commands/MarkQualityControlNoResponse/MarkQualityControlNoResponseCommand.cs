using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.MarkQualityControlNoResponse;

public sealed record MarkQualityControlNoResponseCommand(MarkQualityControlNoResponseRequest Request) : ICommand<QualityControlSaveResult>;

public sealed class MarkQualityControlNoResponseCommandHandler(IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<MarkQualityControlNoResponseCommand, QualityControlSaveResult>
{
    public async Task<QualityControlSaveResult> HandleAsync(MarkQualityControlNoResponseCommand command, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        var request = command.Request;
        if (request.DealId == Guid.Empty || request.RowVersion is { Length: not 8 })
            return new(400, "اطلاعات کنترل کیفیت معتبر نیست. فهرست را به‌روز کنید.");
        return await repository.MarkNoResponseAsync(request, access, dates.PersianNow(), ct);
    }
}
