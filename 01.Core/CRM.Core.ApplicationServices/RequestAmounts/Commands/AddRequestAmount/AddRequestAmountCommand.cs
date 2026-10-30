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

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.AddRequestAmount;
public sealed record AddRequestAmountCommand(RequestAmount Model) : ICommand<RequestAmountOutcome>;
public sealed class AddRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, IApplicationAudit audit) : ICommandHandler<AddRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(AddRequestAmountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.RequestStatusTxt = model.RequestStatus.ToString().Replace("_", " ");
            model.RequestAmountTypeTxt = model.RequestAmountType.ToString().Replace("_", " ");
            model.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            repository.Add(model);
            await repository.SaveAsync(ct);
            await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
            await repository.SaveAsync(ct);
            await repository.CommitAsync(ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await repository.RollbackAsync(CancellationToken.None);
            return new(400);
        }
    }
}
