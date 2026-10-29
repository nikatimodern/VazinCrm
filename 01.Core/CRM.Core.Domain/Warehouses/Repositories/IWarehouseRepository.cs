using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Warehouses.Entities;

namespace CRM.Core.Domain.Warehouses.Repositories;

public interface IWarehouseRepository {
    Task<Warehouse> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsManualWriteBlockedAsync(Guid? warehouseId, Guid[] productIds,
        Guid? voucherId, Guid? transactionId, CancellationToken cancellationToken);
    void Add(Warehouse warehouse);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
