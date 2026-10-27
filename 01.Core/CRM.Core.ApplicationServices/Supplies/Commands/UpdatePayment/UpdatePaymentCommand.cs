using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.UpdatePayment;
public sealed record UpdatePaymentCommand(Payment Model, AppUser User) : ICommand<SupplyResult>;
public sealed class UpdatePaymentCommandHandler(ISupplyWriteRepository repository, ISupplyIntegrationServices integrations) : ICommandHandler<UpdatePaymentCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(UpdatePaymentCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var user = command.User;
        if (model == null)
            return SupplyResult.Invalid();
        await using var transaction = await repository.BeginTransactionAsync(false, cancellationToken);
        try
        {
            model.PaymentDate = integrations.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
            model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = integrations.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;
            var entity = await repository.GetPaymentForUpdateAsync(model.Id, cancellationToken);
            if (entity == null)
                return SupplyResult.Missing("رکوردی یافت نشد!");
            await integrations.LogChangeAsync(entity, model, LogActionEnum.ویرایش, user, cancellationToken, ["PaymentSupplyFactors"]);
            entity.PaymentSupplyFactors = model.PaymentSupplyFactors;
            repository.ApplyPaymentChanges(entity, model);
            var attach = integrations.SaveAttachments(model.Attachments, _AttachType.Payment);
            foreach (var att in attach)
            {
                att.EntityId = model.Id;
            }

            repository.UpdateAttachments(attach);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SupplyResult.Success();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return SupplyResult.Invalid();
        }
    }
}
