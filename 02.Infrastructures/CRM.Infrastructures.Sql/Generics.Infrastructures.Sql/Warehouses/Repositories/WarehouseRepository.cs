using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Warehouses.Repositories;

public class WarehouseRepository(CRMContextDb db) : IWarehouseRepository {
    public Task<Warehouse> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Warehouses.Include(w => w.WarehouseUsers).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> IsManualWriteBlockedAsync(Guid? warehouseId, Guid[] productIds,
        Guid? voucherId, Guid? transactionId, CancellationToken cancellationToken) =>
        db.Warehouses.IgnoreQueryFilters().AnyAsync(w => w.IsMain && (
            w.Id == warehouseId ||
            w.WarehouseProducts.Any(p => productIds.Contains(p.Id) ||
                p.Transactions.Any(t => t.Id == transactionId) ||
                p.WarehouseProductVouchers.Any(v => v.WarehouseVoucherId == voucherId)) ||
            w.WarehouseVouchers.Any(v => v.Id == voucherId)), cancellationToken);

    public void Add(Warehouse warehouse) => db.Warehouses.Add(warehouse);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken);
}
