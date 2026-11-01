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

namespace CRM.Core.ApplicationServices.RequestAmounts.Commands.AddPaymentByRequestAmount;
public sealed record AddPaymentByRequestAmountCommand(Payment Model) : ICommand<RequestAmountOutcome>;
public sealed class AddPaymentByRequestAmountCommandHandler(IRequestAmountRepository repository, CurrentActor actor, RequestAmountWorkflow workflow) : ICommandHandler<AddPaymentByRequestAmountCommand, RequestAmountOutcome>
{
    public async Task<RequestAmountOutcome> HandleAsync(AddPaymentByRequestAmountCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            var validation = await workflow.ValidateRequestAmountsPayment(model, ct);
            if (!validation.IsValid)
                return new(400, validation.Message);
            var isNewPayment = model.Id == Guid.Empty;
            await workflow.AddOrUpdatePayment(model, user, isNewPayment, ct);
            await workflow.SyncRequestAmountsPaymentState(model, user, isNewPayment, ct);
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
