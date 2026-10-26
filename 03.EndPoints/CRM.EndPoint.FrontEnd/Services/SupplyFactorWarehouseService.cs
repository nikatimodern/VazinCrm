using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services;

// All calls run inside the caller's transaction while holding the supply item lock.
public class SupplyFactorWarehouseService(CRMContextDb db) {
    public static readonly Guid WasteId = new("a732c1a0-8710-4b2f-9501-000000000001");
    public static readonly Guid CirculationId = new("a732c1a0-8710-4b2f-9501-000000000002");
    public static readonly Guid StorageId = new("a732c1a0-8710-4b2f-9501-000000000003");
    public static bool IsSystemWarehouse(Guid id) => id == WasteId || id == CirculationId || id == StorageId;
    public static bool IsAllocationDestination(Guid? id) => id == WasteId || id == StorageId;

    public async Task SynchronizeAsync(SupplyFactorItem item, AppUser user) {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("گردش تامین باید داخل تراکنش ثبت شود.");
        var allocations = await db.SupplyFactorItemAllocations.Where(a => a.SupplyFactorItemId == item.Id).ToListAsync();
        var total = item.IsDeleted ? 0 : item.WeightCount;
        var allocated = allocations.Sum(a => a.Weight);
        if (total < allocated || total < 0)
            throw new InvalidOperationException("مقدار تامین از مجموع تخصیص‌ها کمتر است.");
        var targets = new Dictionary<Guid, decimal> {
            [CirculationId] = total - allocated,
            [WasteId] = allocations.Where(a => a.WarehouseId == WasteId).Sum(a => a.Weight),
            [StorageId] = allocations.Where(a => a.WarehouseId == StorageId).Sum(a => a.Weight)
        };
        var products = await db.WarehouseProducts.IgnoreQueryFilters()
            .Where(p => p.SupplyFactorItemId == item.Id).ToListAsync();
        foreach (var (warehouseId, target) in targets) {
            var product = products.FirstOrDefault(p => p.WarehouseId == warehouseId);
            if (product == null && (target > 0 || warehouseId == CirculationId && total > 0)) {
                if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId))
                    throw new InvalidOperationException("انبارهای سیستمی تامین یافت نشدند؛ Migration دیتابیس را اجرا کنید.");
                product = new WarehouseProduct {
                    Id = Guid.NewGuid(), WarehouseId = warehouseId, SupplyFactorItemId = item.Id,
                    ProductCode = item.ProductCode, ProductSmallName = item.ProductSmallName,
                    ProductDescription = item.Product, Unit = item.Unit, UnitTxt = item.UnitTxt,
                    UserCreatorID = user.Id, UserCreator = user.FullName
                };
                db.WarehouseProducts.Add(product);
            }
            if (product == null) continue;
            if (db.Entry(product).State != EntityState.Added)
                await db.WarehouseProducts.FromSqlInterpolated(
                    $"SELECT * FROM [WarehouseProducts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {product.Id}")
                    .IgnoreQueryFilters().LoadAsync();
            if (product.IsDeleted)
                throw new InvalidOperationException("محصول مرتبط با تامین در انبار حذف شده است.");
            if (!item.IsDeleted) {
                product.ProductCode = item.ProductCode;
                product.ProductSmallName = item.ProductSmallName;
                product.ProductDescription = item.Product;
                product.Unit = item.Unit;
                product.UnitTxt = item.UnitTxt;
            }
            var entries = db.WarehouseTransactions.Where(t => t.WarehouseProductId == product.Id && t.SupplyFactorItemId == item.Id);
            var net = await entries.SumAsync(WarehouseExtensions.StockValueExpression);
            if (warehouseId == CirculationId && total > 0 && !await entries.AnyAsync()) {
                AddEntry(product, item, total, user, "ورود اولیه فاکتور تامین");
                net = total;
            }
            var difference = target - net;
            if (difference < 0) {
                var available = await db.GetWarehouseProductAvailableStockAsync(product.Id);
                var pending = db.WarehouseTransactions.Local.Where(t => t.WarehouseProductId == product.Id
                    && db.Entry(t).State == EntityState.Added)
                    .Sum(t => t.TransactionType == WarehouseTransactionTypeEnum.ورود ? t.Weight : -t.Weight);
                if (available + pending < -difference)
                    throw new InvalidOperationException($"موجودی آزاد {product.ProductSmallName} برای اصلاح گردش تامین کافی نیست.");
            }
            if (difference != 0)
                AddEntry(product, item, difference, user, "گردش تخصیص / اصلاح فاکتور تامین");
        }
        await db.SaveChangesAsync();
    }

    private void AddEntry(WarehouseProduct product, SupplyFactorItem item, decimal signedWeight, AppUser user, string description) {
        db.WarehouseTransactions.Add(new WarehouseTransaction {
            Id = Guid.NewGuid(), WarehouseProductId = product.Id, SupplyFactorItemId = item.Id,
            Weight = Math.Abs(signedWeight),
            TransactionType = signedWeight > 0 ? WarehouseTransactionTypeEnum.ورود : WarehouseTransactionTypeEnum.خروج,
            UserCreatorID = user.Id, UserCreator = user.FullName, CommentText = description
        });
    }
}
