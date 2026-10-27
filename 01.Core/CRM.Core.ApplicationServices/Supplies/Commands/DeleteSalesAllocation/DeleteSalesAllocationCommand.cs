using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.DeleteSalesAllocation;
public sealed record DeleteSalesAllocationCommand(SupplyFactorAllocationRequest Model, AppUser User) : ICommand<SupplyResult>;
public sealed class DeleteSalesAllocationCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse) : ICommandHandler<DeleteSalesAllocationCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(DeleteSalesAllocationCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var user = command.User;
        if (model == null || !model.FactorItemId.HasValue)
            return SupplyResult.Invalid();
        await using var transaction = await repository.BeginTransactionAsync(true, cancellationToken);
        var previous = await repository.GetAllocationAsync(model.Id, null, model.FactorItemId, true, cancellationToken);
        if (previous == null)
            return SupplyResult.Missing("تخصیص این آیتم فروش یافت نشد.");
        SupplyFactorItem? source = null;
        if (previous.SupplyFactorItemId.HasValue)
        {
            source = await repository.LockSupplyItemAsync(previous.SupplyFactorItemId, cancellationToken);
            if (source == null)
                return SupplyResult.Missing("آیتم تامین یافت نشد.");
        }

        var sales = await repository.LockSalesItemAsync(model.FactorItemId, cancellationToken);
        if (sales == null || !await repository.SalesFactorExistsAsync(sales.factorId, cancellationToken))
            return SupplyResult.Missing("آیتم فروش یافت نشد.");
        var allocation = await repository.GetAllocationAsync(model.Id, previous.SupplyFactorItemId, sales.Id, false, cancellationToken);
        if (allocation == null || allocation.SupplyFactorItemId != previous.SupplyFactorItemId)
            return SupplyResult.Missing("تخصیص تغییر کرده است؛ اطلاعات را دوباره دریافت کنید.");
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
            if (source != null)
                await supplyWarehouse.SynchronizeAsync(source, user, cancellationToken);
            await supplyWarehouse.ReverseLegacySalesReceiptsAsync(sales, user, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }

        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Success();
    }
}
