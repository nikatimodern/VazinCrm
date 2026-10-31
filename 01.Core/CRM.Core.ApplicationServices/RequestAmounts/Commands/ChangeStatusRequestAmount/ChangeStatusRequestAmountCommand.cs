using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeStatusRequestAmount;
public sealed record ChangeStatusRequestAmountCommand(Guid Id, RequestStatusEnum Status) : ICommand<RequestAmountOutcome>;
public sealed class ChangeStatusRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IRequestAmountEffects effects, RequestAmountWorkflow workflow) : ICommandHandler<ChangeStatusRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(ChangeStatusRequestAmountCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var status = request.Status;
        if (id == Guid.Empty)
            return new(400);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var item = await repository.GetAsync(id, true, ct);
            if (item == null)
                return new(404, "رکوردی یافت نشد!");
            await workflow.ChangeRequestAmountStatus(item, status, user, ct);
            await repository.SaveAsync(ct);
            await effects.NotifyAsync(["مدیر عامل", "مدیر ارشد", "کارشناس مالی"], ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(400);
        }
    }
}
