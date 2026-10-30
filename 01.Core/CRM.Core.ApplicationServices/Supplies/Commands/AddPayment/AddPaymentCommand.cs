using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.AddPayment;
public sealed record AddPaymentCommand(Payment Model) : ICommand<SupplyResult>;
public sealed class AddPaymentCommandHandler(ISupplyWriteRepository repository, ISupplyIntegrationServices integrations, CurrentActor actor) : ICommandHandler<AddPaymentCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(AddPaymentCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var current = await actor.FindAsync(cancellationToken);
        if (current == null)
            return SupplyResult.Unauthenticated();
        var user = current.User;
        if (model == null)
            return SupplyResult.Invalid();
        await using var transaction = await repository.BeginTransactionAsync(false, cancellationToken);
        try
        {
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.PaymentDate = integrations.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
            model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");
            await repository.AddPaymentAsync(model, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            var attach = integrations.SaveAttachments(model.Attachments, _AttachType.Payment);
            foreach (var att in attach)
            {
                att.EntityId = model.Id;
            }

            await repository.AddAttachmentsAsync(attach, cancellationToken);
            await integrations.LogChangeAsync(model, null, LogActionEnum.ثبت, user, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SupplyResult.Success();
        }
        catch (Exception)when (!cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return SupplyResult.Invalid();
        }
    }
}
