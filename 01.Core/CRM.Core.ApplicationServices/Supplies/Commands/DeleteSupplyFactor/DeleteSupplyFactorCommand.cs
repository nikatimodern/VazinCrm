using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.DeleteSupplyFactor;
public sealed record DeleteSupplyFactorCommand(Guid Id, AppUser User) : ICommand<SupplyResult>;
public sealed class DeleteSupplyFactorCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse, ISupplyIntegrationServices integrations) : ICommandHandler<DeleteSupplyFactorCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(DeleteSupplyFactorCommand command, CancellationToken cancellationToken)
    {
        var id = command.Id;
        var user = command.User;
        await using var transaction = await repository.BeginTransactionAsync(true, cancellationToken);
        var item = await repository.GetSupplyFactorForUpdateAsync(id, cancellationToken);
        if (item == null)
            return SupplyResult.Missing();
        foreach (var supplyItem in item.Items.OrderBy(i => i.Id))
        {
            await repository.LockSupplyItemAsync(supplyItem.Id, cancellationToken);
            if (await repository.HasAllocationsAsync(supplyItem.Id, cancellationToken))
                return SupplyResult.Invalid("برای حذف فاکتور تامین، ابتدا تخصیص‌های آن را حذف کنید.");
            supplyItem.IsDeleted = true;
        }

        item.IsDeleted = true;
        item.ModifyDate = DateTime.Now;
        item.ModifyDateTxt = integrations.GetDateTimePersian();
        item.UserModify = user.FullName;
        item.UserModifyID = user.Id;
        await integrations.LogChangeAsync(item, null, LogActionEnum.حذف, user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        try
        {
            foreach (var supplyItem in item.Items.OrderBy(i => i.Id))
                await supplyWarehouse.SynchronizeAsync(supplyItem, user, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }

        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Success();
    }
}
