using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Core.Domain.Supplies.Rules;
using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;
public sealed class SupplyWriteRepository(CRMContextDb db) : ISupplyWriteRepository
{
    public async Task<ISupplyTransaction> BeginTransactionAsync(bool serializable, CancellationToken ct) => new SupplyTransaction(await db.Database.BeginTransactionAsync(serializable ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted, ct));
    public Task<SupplyFactorItem> LockSupplyItemAsync(Guid? id, CancellationToken ct) => db.SupplyFactorItems.FromSqlInterpolated($"SELECT * FROM [SupplyFactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync(ct);
    public Task<FactorItem> LockSalesItemAsync(Guid? id, CancellationToken ct) => db.FactorItems.FromSqlInterpolated($"SELECT * FROM [FactorItems] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync(ct);
    public Task<bool> SupplyFactorExistsAsync(Guid id, CancellationToken ct) => db.SupplyFactors.AnyAsync(f => f.Id == id, ct);
    public Task<bool> SalesFactorExistsAsync(Guid id, CancellationToken ct) => db.Factors.AnyAsync(f => f.Id == id, ct);
    public Task<bool> WarehouseExistsAsync(Guid? id, CancellationToken ct) => db.Warehouses.AnyAsync(w => w.Id == id && ((w.Id == SupplyAllocationRules.WasteId && w.WarehouseType == WarehouseType.انبار_ضایعات) || (w.Id == SupplyAllocationRules.StorageId && w.WarehouseType == WarehouseType.انبار_ذخیره)), ct);
    public Task<WarehouseProduct> GetWarehouseSourceAsync(Guid? id, CancellationToken ct) => db.WarehouseProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.SupplyFactorItemId != null, ct);
    public Task<SupplyFactorItemAllocation> GetAllocationAsync(Guid id, Guid? sourceId, Guid? salesId, bool detached, CancellationToken ct)
    {
        var query = db.SupplyFactorItemAllocations.AsQueryable();
        if (detached)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(a => a.Id == id && (!sourceId.HasValue || a.SupplyFactorItemId == sourceId) && (!salesId.HasValue || a.FactorItemId == salesId), ct);
    }

    public Task<decimal> SumAllocationsAsync(Guid? sourceId, Guid? salesId, Guid excludedId, CancellationToken ct) => db.SupplyFactorItemAllocations.Where(a => (!sourceId.HasValue || a.SupplyFactorItemId == sourceId) && (!salesId.HasValue || a.FactorItemId == salesId) && a.Id != excludedId).SumAsync(a => a.Weight, ct);
    public Task<bool> AllocationExistsAsync(Guid? sourceId, Guid? salesId, Guid? warehouseId, Guid? warehouseProductId, Guid excludedId, CancellationToken ct) => db.SupplyFactorItemAllocations.AnyAsync(a => a.SupplyFactorItemId == sourceId && a.FactorItemId == salesId && a.WarehouseId == warehouseId && a.SourceWarehouseProductId == warehouseProductId && a.Id != excludedId, ct);
    public Task<bool> HasAllocationsAsync(Guid sourceId, CancellationToken ct) => db.SupplyFactorItemAllocations.AnyAsync(a => a.SupplyFactorItemId == sourceId, ct);
    public void AddAllocation(SupplyFactorItemAllocation allocation) => db.SupplyFactorItemAllocations.Add(allocation);
    public void RemoveAllocation(SupplyFactorItemAllocation allocation) => db.SupplyFactorItemAllocations.Remove(allocation);
    public Task<SupplyFactor> GetSupplyFactorForUpdateAsync(Guid id, CancellationToken ct) => db.SupplyFactors.Include(f => f.Items).Include(f => f.SupplyFactorOrderHeads).FirstOrDefaultAsync(f => f.Id == id, ct);
    public async Task AddSupplyFactorAsync(SupplyFactor factor, CancellationToken ct) => await db.SupplyFactors.AddAsync(factor, ct);
    public Task<Payment> GetPaymentForUpdateAsync(Guid id, CancellationToken ct) => db.Payments.Include(p => p.PaymentSupplyFactors).FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task AddPaymentAsync(Payment payment, CancellationToken ct) => await db.Payments.AddAsync(payment, ct);
    public Task AddAttachmentsAsync(IList<Attachment> attachments, CancellationToken ct) => db.Attachments.AddRangeAsync(attachments, ct);
    public void UpdateAttachments(IList<Attachment> attachments) => db.Attachments.UpdateRange(attachments);
    public void ApplyFactorChanges(SupplyFactor target, SupplyFactor source) => db.Entry(target).CurrentValues.SetValues(source);
    public void ApplyItemChanges(SupplyFactorItem target, SupplyFactorItem source) => db.Entry(target).CurrentValues.SetValues(source);
    public void ApplyPaymentChanges(Payment target, Payment source) => db.Entry(target).CurrentValues.SetValues(source);
    public Task<ASetting> GetSettingsAsync(CancellationToken ct) => db.ASettings.FirstOrDefaultAsync(ct);
    public Task<Supply> GetSupplyForAuditAsync(Guid id, CancellationToken ct) => db.Supplies.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
    public void AddSupply(Supply supply) => db.Supplies.Add(supply);
    public void UpdateSupply(Supply supply) => db.Supplies.Update(supply);
    public Task<PreFactor> GetPreFactorForUpdateAsync(Guid id, CancellationToken ct) => db.PreFactors.FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
    private sealed class SupplyTransaction(IDbContextTransaction transaction) : ISupplyTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct) => transaction.RollbackAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
