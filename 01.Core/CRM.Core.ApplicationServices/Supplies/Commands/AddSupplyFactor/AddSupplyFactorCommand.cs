using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.AddSupplyFactor;
public sealed record AddSupplyFactorCommand(SupplyFactor Model) : ICommand<SupplyResult>;
public sealed class AddSupplyFactorCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse, ISupplyIntegrationServices integrations, CurrentActor actor) : ICommandHandler<AddSupplyFactorCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(AddSupplyFactorCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var current = await actor.FindAsync(cancellationToken);
        if (current == null)
            return SupplyResult.Unauthenticated();
        var user = current.User;
        if (model?.Items == null || !model.Items.Any() || model.Items.Any(i => i.WeightCount <= 0 || i.IsDeleted))
            return SupplyResult.Invalid("آیتم‌های تامین و مقدار مثبت آن‌ها الزامی است.");
        await using var transaction = await repository.BeginTransactionAsync(false, cancellationToken);
        try
        {
            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.DateMiladi = integrations.GetDateMiladi(model.Date) ?? DateTime.Now;
            await repository.AddSupplyFactorAsync(model, cancellationToken);
            var attach = integrations.SaveAttachments(model.Attachments, _AttachType.SupplyFactor);
            foreach (var att in attach)
            {
                att.EntityId = model.Id;
            }

            await repository.AddAttachmentsAsync(attach, cancellationToken);
            await integrations.LogChangeAsync(model, null, LogActionEnum.ثبت, user, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            foreach (var supplyItem in model.Items.OrderBy(i => i.Id))
                await supplyWarehouse.SynchronizeAsync(supplyItem, user, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SupplyResult.Success();
        }
        catch (Exception e)when (!cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return SupplyResult.Invalid(e is InvalidOperationException ? e.Message : "ثبت فاکتور تامین انجام نشد.");
        }
    }
}
