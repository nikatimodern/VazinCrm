using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.DeleteQualityControl;

public sealed record DeleteQualityControlCommand(DeleteQualityControlRequest Request) : ICommand<QualityControlSaveResult>;

public sealed class DeleteQualityControlCommandHandler(IQualityControlRepository repository, CurrentActor actor)
    : ICommandHandler<DeleteQualityControlCommand, QualityControlSaveResult>
{
    public async Task<QualityControlSaveResult> HandleAsync(DeleteQualityControlCommand command, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        if (command.Request.DealId == Guid.Empty || command.Request.RowVersion is not { Length: 8 })
            return new(400, "اطلاعات حذف کنترل کیفیت معتبر نیست. فهرست را به‌روز کنید.");
        return await repository.DeleteAsync(command.Request, access, ct);
    }
}
