using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Core.Domain.Supplies.Rules;

namespace CRM.Core.ApplicationServices.Supplies.Commands.SaveSalesAllocation;
public sealed record SaveSalesAllocationCommand(SupplyFactorAllocationRequest Model, Guid? AllocationId) : ICommand<SupplyResult>;
public sealed class SaveSalesAllocationCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse, CurrentActor actor) : ICommandHandler<SaveSalesAllocationCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(SaveSalesAllocationCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var allocationId = command.AllocationId;
        var current = await actor.FindAsync(cancellationToken);
        if (current == null)
            return SupplyResult.Unauthenticated();
        var user = current.User;
        if (model == null || !SupplyAllocationRules.IsValidWeight(model.Weight))
            return SupplyResult.Invalid("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        if (!model.FactorItemId.HasValue || model.FactorItemId == Guid.Empty)
            return SupplyResult.Invalid("آیتم فروش معتبر نیست.");
        var warehouse = model.SourceWarehouseProductId.HasValue;
        if (!warehouse && (model.WarehouseId.HasValue || model.SupplyFactorItemId == Guid.Empty) || warehouse && model.SupplyFactorItemId != Guid.Empty)
            return SupplyResult.Invalid("یک آیتم تامین یا موجودی مشخصی از انبار ضایعات / ذخیره انتخاب کنید.");
        await using var transaction = await repository.BeginTransactionAsync(true, cancellationToken);
        var previous = allocationId.HasValue ? await repository.GetAllocationAsync(allocationId.Value, null, model.FactorItemId, true, cancellationToken) : null;
        if (allocationId.HasValue && previous == null)
            return SupplyResult.Missing("تخصیص این آیتم فروش یافت نشد.");
        var warehouseProduct = warehouse ? await repository.GetWarehouseSourceAsync(model.SourceWarehouseProductId, cancellationToken) : null;
        if (warehouse && (warehouseProduct == null || !SupplyAllocationRules.IsAllocationDestination(warehouseProduct.WarehouseId) || model.WarehouseId.HasValue && model.WarehouseId != warehouseProduct.WarehouseId))
            return SupplyResult.Invalid("موجودی انبار مرتبط با آیتم تامین یافت نشد.");
        Guid? targetSourceId = warehouseProduct?.SupplyFactorItemId ?? model.SupplyFactorItemId;
        var sources = new List<SupplyFactorItem>();
        // The source-before-sales lock order is shared with the existing supply workspace.
        foreach (var sourceId in new[]
        {
            targetSourceId,
            previous?.SupplyFactorItemId
        }.Where(id => id.HasValue).Distinct().OrderBy(id => id))
        {
            var source = await repository.LockSupplyItemAsync(sourceId, cancellationToken);
            if (source == null || !await repository.SupplyFactorExistsAsync(source.SupplyFactorId, cancellationToken))
                return SupplyResult.Missing("آیتم تامین یافت نشد.");
            sources.Add(source);
        }

        var sales = await repository.LockSalesItemAsync(model.FactorItemId, cancellationToken);
        if (sales == null || !await repository.SalesFactorExistsAsync(sales.factorId, cancellationToken))
            return SupplyResult.Missing("آیتم فروش یافت نشد.");
        if (!SupplyAllocationRules.HaveSameUnit((int)sources.Single(i => i.Id == targetSourceId).Unit, sales.Unit) || warehouseProduct != null && !SupplyAllocationRules.HaveSameUnit((int)warehouseProduct.Unit, sales.Unit))
            return SupplyResult.Invalid("واحد آیتم فروش باید با واحد آیتم تامین و موجودی انبار انتخاب‌شده یکسان باشد.");
        if (!SupplyAllocationRules.TryParseSalesWeight(sales.WeightCount, out var total) || total <= 0)
            return SupplyResult.Invalid("مقدار آیتم فروش معتبر نیست؛ ابتدا فاکتور فروش را اصلاح کنید.");
        var excluded = allocationId ?? Guid.Empty;
        var allocation = allocationId.HasValue ? await repository.GetAllocationAsync(excluded, null, sales.Id, false, cancellationToken) : null;
        if (allocationId.HasValue && (allocation == null || previous == null || allocation.SupplyFactorItemId != previous.SupplyFactorItemId))
            return SupplyResult.Invalid("تخصیص تغییر کرده است؛ اطلاعات را دوباره دریافت کنید.");
        var salesAllocated = await repository.SumAllocationsAsync(null, sales.Id, excluded, cancellationToken);
        if (!SupplyAllocationRules.HasCapacity(model.Weight, total, salesAllocated))
            return SupplyResult.Invalid("مقدار از باقی‌مانده آیتم فروش در مجموع تمام تخصیص‌ها بیشتر است.");
        if (await repository.AllocationExistsAsync(targetSourceId, sales.Id, null, model.SourceWarehouseProductId, excluded, cancellationToken))
            return SupplyResult.Invalid("این آیتم تامین یا انبار قبلاً ثبت شده است؛ تخصیص قبلی را ویرایش کنید.");
        if (!warehouse && targetSourceId.HasValue)
        {
            var sourceAllocated = await repository.SumAllocationsAsync(targetSourceId, null, excluded, cancellationToken);
            if (previous?.SourceWarehouseProductId != null && previous.SupplyFactorItemId == targetSourceId)
                sourceAllocated += previous.Weight; // Returning the old withdrawal restores its warehouse remainder.
            if (!SupplyAllocationRules.HasCapacity(model.Weight, sources.Single(i => i.Id == targetSourceId).WeightCount, sourceAllocated))
                return SupplyResult.Invalid("مقدار از باقی‌مانده آیتم تامین در مجموع تمام تخصیص‌ها بیشتر است.");
        }

        try
        {
            await supplyWarehouse.ChangeWarehouseSourceAsync(previous?.SourceWarehouseProductId, previous?.Weight ?? 0, model.SourceWarehouseProductId, model.Weight, cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }

        if (allocation == null)
        {
            allocation = new SupplyFactorItemAllocation();
            repository.AddAllocation(allocation);
        }

        allocation.FactorItemId = sales.Id;
        allocation.SupplyFactorItemId = targetSourceId;
        allocation.WarehouseId = null;
        allocation.SourceWarehouseProductId = model.SourceWarehouseProductId;
        allocation.Weight = model.Weight;
        await repository.SaveChangesAsync(cancellationToken);
        try
        {
            foreach (var source in sources.OrderBy(i => i.Id))
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
