using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Warehouses.Entities;

namespace CRM.Core.Domain.Supplies.Repositories;
public interface ISupplyTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}

public interface ISupplyWriteRepository
{
    Task<ISupplyTransaction> BeginTransactionAsync(bool serializable, CancellationToken cancellationToken);
    Task<SupplyFactorItem> LockSupplyItemAsync(Guid? id, CancellationToken cancellationToken);
    Task<FactorItem> LockSalesItemAsync(Guid? id, CancellationToken cancellationToken);
    Task<bool> SupplyFactorExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SalesFactorExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> WarehouseExistsAsync(Guid? id, CancellationToken cancellationToken);
    Task<WarehouseProduct> GetWarehouseSourceAsync(Guid? id, CancellationToken cancellationToken);
    Task<SupplyFactorItemAllocation> GetAllocationAsync(Guid id, Guid? sourceId, Guid? salesId, bool detached, CancellationToken cancellationToken);
    Task<decimal> SumAllocationsAsync(Guid? sourceId, Guid? salesId, Guid excludedId, CancellationToken cancellationToken);
    Task<bool> AllocationExistsAsync(Guid? sourceId, Guid? salesId, Guid? warehouseId, Guid? warehouseProductId, Guid excludedId, CancellationToken cancellationToken);
    Task<bool> HasAllocationsAsync(Guid sourceId, CancellationToken cancellationToken);
    void AddAllocation(SupplyFactorItemAllocation allocation);
    void RemoveAllocation(SupplyFactorItemAllocation allocation);
    Task<SupplyFactor> GetSupplyFactorForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task AddSupplyFactorAsync(SupplyFactor factor, CancellationToken cancellationToken);
    Task<Payment> GetPaymentForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken);
    Task AddAttachmentsAsync(IList<Attachment> attachments, CancellationToken cancellationToken);
    void UpdateAttachments(IList<Attachment> attachments);
    void ApplyFactorChanges(SupplyFactor target, SupplyFactor source);
    void ApplyItemChanges(SupplyFactorItem target, SupplyFactorItem source);
    void ApplyPaymentChanges(Payment target, Payment source);
    Task<ASetting> GetSettingsAsync(CancellationToken cancellationToken);
    Task<Supply> GetSupplyForAuditAsync(Guid id, CancellationToken cancellationToken);
    void AddSupply(Supply supply);
    void UpdateSupply(Supply supply);
    Task<PreFactor> GetPreFactorForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ISupplyInventoryRepository
{
    Task SynchronizeAsync(SupplyFactorItem item, AppUser user, CancellationToken cancellationToken);
    Task ChangeWarehouseSourceAsync(Guid? previousProductId, decimal previousWeight, Guid? targetProductId, decimal targetWeight, CancellationToken cancellationToken);
    Task ReverseLegacySalesReceiptsAsync(FactorItem item, AppUser user, CancellationToken cancellationToken);
}
