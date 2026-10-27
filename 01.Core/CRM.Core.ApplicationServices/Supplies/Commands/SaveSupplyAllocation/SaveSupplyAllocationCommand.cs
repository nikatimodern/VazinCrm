using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Core.Domain.Supplies.Rules;

namespace CRM.Core.ApplicationServices.Supplies.Commands.SaveSupplyAllocation;
public sealed record SaveSupplyAllocationCommand(SupplyFactorAllocationRequest Model, Guid? AllocationId, AppUser User) : ICommand<SupplyResult>;
public sealed class SaveSupplyAllocationCommandHandler(ISupplyWriteRepository repository, ISupplyInventoryRepository supplyWarehouse) : ICommandHandler<SaveSupplyAllocationCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(SaveSupplyAllocationCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var allocationId = command.AllocationId;
        var user = command.User;
        try
        {
            if (model == null || !SupplyAllocationRules.IsValidWeight(model.Weight))
                return SupplyResult.Invalid("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
            var toWarehouse = model.WarehouseId.HasValue;
            if (toWarehouse == model.FactorItemId.HasValue || model.FactorItemId == Guid.Empty || toWarehouse && !SupplyAllocationRules.IsAllocationDestination(model.WarehouseId))
                return SupplyResult.Invalid("فقط یک مقصد معتبر (آیتم فروش، انبار ضایعات یا انبار ذخیره) انتخاب کنید.");
            if (toWarehouse && !await repository.WarehouseExistsAsync(model.WarehouseId, cancellationToken))
                return SupplyResult.Invalid("انبار مقصد معتبر نیست.");
            await using var transaction = await repository.BeginTransactionAsync(true, cancellationToken);
            // Serialize allocations for the same supply item, including concurrent requests.
            var item = await repository.LockSupplyItemAsync(model.SupplyFactorItemId, cancellationToken);
            if (item == null || !await repository.SupplyFactorExistsAsync(item.SupplyFactorId, cancellationToken))
                return SupplyResult.Missing("آیتم تامین یافت نشد.");
            decimal salesWeight = 0;
            if (!toWarehouse)
            {
                // Serialize requests from different supply items targeting the same sales item.
                var salesItem = await repository.LockSalesItemAsync(model.FactorItemId, cancellationToken);
                if (salesItem == null || !await repository.SalesFactorExistsAsync(salesItem.factorId, cancellationToken))
                    return SupplyResult.Invalid("آیتم فاکتور فروش یافت نشد.");
                if (!SupplyAllocationRules.HaveSameUnit(salesItem.Unit, (int)item.Unit))
                    return SupplyResult.Invalid("واحد آیتم فروش و آیتم تامین باید یکسان باشد.");
                if (!SupplyAllocationRules.TryParseSalesWeight(salesItem.WeightCount, out salesWeight) || salesWeight <= 0)
                    return SupplyResult.Invalid("مقدار آیتم فاکتور فروش معتبر نیست؛ ابتدا فاکتور فروش را اصلاح کنید.");
            }

            SupplyFactorItemAllocation? allocation = null;
            if (allocationId.HasValue)
            {
                allocation = await repository.GetAllocationAsync(allocationId.Value, item.Id, null, false, cancellationToken);
                if (allocation == null)
                    return SupplyResult.Missing("تخصیص یافت نشد؛ ممکن است قبلاً حذف شده باشد.");
            }

            var excludedId = allocationId ?? Guid.Empty;
            if (!toWarehouse)
            {
                var salesAllocated = await repository.SumAllocationsAsync(null, model.FactorItemId, excludedId, cancellationToken);
                if (!SupplyAllocationRules.HasCapacity(model.Weight, salesWeight, salesAllocated))
                    return SupplyResult.Invalid("مقدار تخصیص از باقی‌مانده آیتم فروش در مجموع تمام فاکتورهای تامین بیشتر است.");
            }

            if (await repository.AllocationExistsAsync(item.Id, model.FactorItemId, model.WarehouseId, toWarehouse ? null : allocation?.SourceWarehouseProductId, excludedId, cancellationToken))
                return SupplyResult.Invalid("این مقصد قبلاً به همین آیتم تامین تخصیص داده شده است؛ تخصیص قبلی را ویرایش کنید.");
            var allocated = await repository.SumAllocationsAsync(item.Id, null, excludedId, cancellationToken);
            var warehouseSource = allocation?.SourceWarehouseProductId;
            if (warehouseSource == null && !SupplyAllocationRules.HasCapacity(model.Weight, item.WeightCount, allocated))
                return SupplyResult.Invalid("مقدار تخصیص از مقدار باقی‌مانده آیتم تامین بیشتر است.");
            if (warehouseSource != null && toWarehouse)
                return SupplyResult.Invalid("برای بازگرداندن برداشت به انبار، تخصیص فروش را حذف کنید.");
            if (warehouseSource != null && allocation != null)
            {
                try
                {
                    await supplyWarehouse.ChangeWarehouseSourceAsync(warehouseSource, allocation.Weight, toWarehouse ? null : warehouseSource, model.Weight, cancellationToken);
                }
                catch (InvalidOperationException e)
                {
                    return SupplyResult.Invalid(e.Message);
                }

                if (toWarehouse)
                    allocation.SourceWarehouseProductId = null;
            }

            if (allocation == null)
            {
                repository.AddAllocation(new SupplyFactorItemAllocation { SupplyFactorItemId = item.Id, FactorItemId = model.FactorItemId, WarehouseId = model.WarehouseId, Weight = model.Weight });
            }
            else
            {
                allocation.FactorItemId = model.FactorItemId;
                allocation.WarehouseId = model.WarehouseId;
                allocation.Weight = model.Weight;
            }

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
        catch (InvalidOperationException e)
        {
            return SupplyResult.Invalid(e.Message);
        }
    }
}
