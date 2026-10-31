using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.DeleteSupplyAllocation;
public sealed record DeleteSupplyAllocationCommand(SupplyFactorAllocationRequest Model) : ICommand<SupplyResult>;
public sealed class DeleteSupplyAllocationCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse, CurrentActor actor) : ICommandHandler<DeleteSupplyAllocationCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(DeleteSupplyAllocationCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var current = await actor.FindAsync(cancellationToken);
        if (current == null)
            return SupplyResult.Unauthenticated();
        var user = current.User;
        if (model == null)
            return SupplyResult.Invalid();
        await using var transaction = await repository.BeginTransactionAsync(true, cancellationToken);
        var item = await repository.LockSupplyItemAsync(model.SupplyFactorItemId, cancellationToken);
        if (item == null || !await repository.SupplyFactorExistsAsync(item.SupplyFactorId, cancellationToken))
            return SupplyResult.Missing("آیتم تامین یافت نشد.");
        var allocation = await repository.GetAllocationAsync(model.Id, item.Id, null, false, cancellationToken);
        if (allocation == null)
            return SupplyResult.Missing("تخصیص یافت نشد؛ ممکن است قبلاً حذف شده باشد.");
        try
        {
            await supplyWarehouse.ChangeWarehouseSourceAsync(allocation.SourceWarehouseProductId, allocation.Weight, null, 0, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }

        repository.RemoveAllocation(allocation);
        await repository.SaveChangesAsync(cancellationToken);
        try
        {
            await supplyWarehouse.SynchronizeAsync(item, user, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }

        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Success(new { supplyFactorId = item.SupplyFactorId });
    }
}
