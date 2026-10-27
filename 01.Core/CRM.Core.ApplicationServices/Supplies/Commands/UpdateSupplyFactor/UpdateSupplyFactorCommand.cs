using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.UpdateSupplyFactor;
public sealed record UpdateSupplyFactorCommand(SupplyFactor Model, AppUser User) : ICommand<SupplyResult>;
public sealed class UpdateSupplyFactorCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse, ISupplyIntegrationServices integrations) : ICommandHandler<UpdateSupplyFactorCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(UpdateSupplyFactorCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var user = command.User;
        if (model == null)
            return SupplyResult.Invalid();
        await using var transaction = await repository.BeginTransactionAsync(false, cancellationToken);
        try
        {
            model.DateMiladi = integrations.GetDateMiladi(model.Date) ?? DateTime.Now;
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = integrations.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;
            var entity = await repository.GetSupplyFactorForUpdateAsync(model.Id, cancellationToken);
            if (entity == null)
                return SupplyResult.Missing("رکوردی یافت نشد!");
            foreach (var existingItem in entity.Items.OrderBy(x => x.Id))
            {
                await repository.LockSupplyItemAsync(existingItem.Id, cancellationToken);
                var allocated = await repository.SumAllocationsAsync(existingItem.Id, null, Guid.Empty, cancellationToken);
                var replacement = model.Items?.FirstOrDefault(x => x.Id == existingItem.Id && !x.IsDeleted);
                if (allocated > 0 && (replacement == null || replacement.WeightCount < allocated))
                    return SupplyResult.Invalid("آیتم دارای تخصیص را نمی‌توان حذف کرد یا مقدار آن را کمتر از مجموع تخصیص‌ها قرار داد.");
                if (allocated > 0 && replacement != null && (replacement.ProductCode != existingItem.ProductCode || replacement.Product != existingItem.Product || replacement.Unit != existingItem.Unit))
                    return SupplyResult.Invalid("برای تغییر محصول یا واحد آیتم، ابتدا تخصیص‌های آن را حذف کنید.");
            }

            await integrations.LogChangeAsync(entity, model, LogActionEnum.ویرایش, user, cancellationToken, ["SupplyFactorOrderHeads", "Items"]);
            repository.ApplyFactorChanges(entity, model);
            entity.SupplyFactorOrderHeads = model.SupplyFactorOrderHeads;
            var incomingItems = model.Items ?? [];
            if (incomingItems.Any(i => i.Id != Guid.Empty && !entity.Items.Any(existing => existing.Id == i.Id)))
                return SupplyResult.Invalid("آیتم ارسالی متعلق به این فاکتور تامین نیست.");
            if (incomingItems.Any(i => !i.IsDeleted && i.WeightCount <= 0))
                return SupplyResult.Invalid("مقدار آیتم تامین باید مثبت باشد.");
            foreach (var existingItem in entity.Items.ToList())
            {
                var replacement = incomingItems.FirstOrDefault(i => i.Id == existingItem.Id && !i.IsDeleted);
                if (replacement == null)
                    existingItem.IsDeleted = true;
                else
                {
                    repository.ApplyItemChanges(existingItem, replacement);
                    existingItem.SupplyFactorId = entity.Id;
                }
            }

            foreach (var added in incomingItems.Where(i => i.Id == Guid.Empty && !i.IsDeleted))
            {
                added.SupplyFactorId = entity.Id;
                entity.Items.Add(added);
            }

            var attach = integrations.SaveAttachments(model.Attachments, _AttachType.SupplyFactor, false);
            foreach (var att in attach.Where(f => f.EntityId == null))
            {
                att.EntityId = model.Id;
            }

            repository.UpdateAttachments(attach);
            await repository.SaveChangesAsync(cancellationToken);
            foreach (var supplyItem in entity.Items.OrderBy(i => i.Id))
                await supplyWarehouse.SynchronizeAsync(supplyItem, user, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SupplyResult.Success();
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return SupplyResult.Invalid(e is InvalidOperationException ? e.Message : "ویرایش فاکتور تامین انجام نشد.");
        }
    }
}
