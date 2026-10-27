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

    // Move weight between the warehouse remainder and its sales allocation. The supply total
    // is unchanged: the same weight is never counted twice as both warehouse stock and a sale.
    public async Task ChangeWarehouseSourceAsync(Guid? previousProductId, decimal previousWeight,
        Guid? targetProductId, decimal targetWeight) {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("برداشت انبار باید داخل تراکنش ثبت شود.");
        var products = new Dictionary<Guid, WarehouseProduct>();
        foreach (var id in new[] { previousProductId, targetProductId }.Where(id => id.HasValue).Distinct().OrderBy(id => id)) {
            var product = await db.WarehouseProducts.FromSqlInterpolated(
                $"SELECT * FROM [WarehouseProducts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync();
            if (product == null || !product.SupplyFactorItemId.HasValue || !IsAllocationDestination(product.WarehouseId)
                || !await db.Warehouses.AnyAsync(w => w.Id == product.WarehouseId
                    && ((w.Id == WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات)
                        || (w.Id == StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره))))
                throw new InvalidOperationException("موجودی انبار مرتبط با آیتم تامین یافت نشد.");
            products.Add(product.Id, product);
        }
        if (targetProductId.HasValue) {
            var product = products[targetProductId.Value];
            var restored = targetProductId == previousProductId ? previousWeight : 0;
            var available = await db.GetWarehouseProductAvailableStockAsync(product.Id) + restored;
            var remainder = await db.SupplyFactorItemAllocations.Where(a => a.SupplyFactorItemId == product.SupplyFactorItemId
                && a.WarehouseId == product.WarehouseId && a.FactorItemId == null)
                .SumAsync(a => (decimal?)a.Weight) ?? 0;
            if (targetWeight > Math.Min(available, remainder + restored))
                throw new InvalidOperationException("مقدار از موجودی آزاد و قابل تخصیص این آیتم در انبار بیشتر است.");
        }
        foreach (var product in products.Values) {
            var difference = (product.Id == previousProductId ? previousWeight : 0)
                - (product.Id == targetProductId ? targetWeight : 0);
            if (difference == 0) continue;
            var transfer = await db.SupplyFactorItemAllocations.FirstOrDefaultAsync(a =>
                a.SupplyFactorItemId == product.SupplyFactorItemId && a.WarehouseId == product.WarehouseId && a.FactorItemId == null);
            var weight = (transfer?.Weight ?? 0) + difference;
            if (weight < 0) throw new InvalidOperationException("باقی‌مانده تخصیص انبار کافی نیست.");
            if (weight == 0) { if (transfer != null) db.SupplyFactorItemAllocations.Remove(transfer); }
            else if (transfer != null) transfer.Weight = weight;
            else db.SupplyFactorItemAllocations.Add(new SupplyFactorItemAllocation {
                SupplyFactorItemId = product.SupplyFactorItemId, WarehouseId = product.WarehouseId, Weight = weight
            });
        }
    }

    // Reverse historical sales receipts only after their unlinked allocation is corrected/deleted.
    // New sales allocations never create stock or a product owned by the sales item.
    public async Task ReverseLegacySalesReceiptsAsync(CRM.Core.Domain.Factors.Entities.FactorItem item, AppUser user) {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("اصلاح گردش انبار باید داخل تراکنش ثبت شود.");
        var pending = await db.SupplyFactorItemAllocations.Where(a => a.FactorItemId == item.Id
            && a.SupplyFactorItemId == null && a.WarehouseId != null).Select(a => a.WarehouseId).ToListAsync();
        var products = await db.WarehouseProducts.IgnoreQueryFilters().Where(p => p.FactorItemId == item.Id).ToListAsync();
        foreach (var product in products) {
            if (pending.Contains(product.WarehouseId)) continue;
            await db.WarehouseProducts.FromSqlInterpolated(
                $"SELECT * FROM [WarehouseProducts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {product.Id}").IgnoreQueryFilters().LoadAsync();
            var net = await db.WarehouseTransactions.Where(t => t.WarehouseProductId == product.Id && t.FactorItemId == item.Id)
                .SumAsync(WarehouseExtensions.StockValueExpression);
            if (net == 0) continue;
            if (product.IsDeleted || net < 0 || await db.GetWarehouseProductAvailableStockAsync(product.Id) < net)
                throw new InvalidOperationException("موجودی آزاد برای اصلاح ورود اشتباه قبلی کافی نیست.");
            db.WarehouseTransactions.Add(new WarehouseTransaction {
                Id = Guid.NewGuid(), WarehouseProductId = product.Id, FactorItemId = item.Id,
                Weight = net, TransactionType = WarehouseTransactionTypeEnum.خروج,
                UserCreatorID = user.Id, UserCreator = user.FullName, CommentText = "اصلاح ورود اشتباه تخصیص فروش"
            });
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
