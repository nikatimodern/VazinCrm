using System.Data;
using CRM.Core.Domain.Taxation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxationRepository(CRMContextDb db, ITaxPreviewProtector protection) : ITaxationRepository
{
    public async Task<IReadOnlyList<TaxMemory>> ListMemoriesAsync(CancellationToken ct) =>
        await db.TaxMemories.AsNoTracking().OrderByDescending(x => x.IsActive).ThenBy(x => x.FiscalId).ToListAsync(ct);

    public async Task<Guid> SaveMemoryAsync(Guid? id, string fiscalId, string title, CancellationToken ct)
    {
        var memory = id.HasValue
            ? await db.TaxMemories.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException()
            : new TaxMemory { Id = Guid.NewGuid(), FiscalId = fiscalId };
        if (memory.FiscalId != fiscalId)
            throw new ArgumentException("برای شناسه مالیاتی جدید یک حافظه مستقل تعریف کنید.");
        if (!id.HasValue) db.TaxMemories.Add(memory);
        memory.Title = title;
        memory.UpdatedUtc = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            throw new ArgumentException("شناسه حافظه قبلاً ثبت شده است.", ex);
        }
        return memory.Id;
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.TaxMemories.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException();
        var now = DateTime.UtcNow;
        await db.TaxMemories.Where(x => x.IsActive && x.Id != id).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedUtc, now), ct);
        await db.TaxMemories.Where(x => x.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsActive, true).SetProperty(x => x.UpdatedUtc, now), ct);
        await tx.CommitAsync(ct);
    }

    public async Task<TaxPreviewSource> GetPreviewSourceAsync(Guid factorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var factor = await db.Factors.AsNoTracking().Include(x => x.FactorItems)
            .SingleOrDefaultAsync(x => x.Id == factorId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException();
        var ids = factor.FactorItems.Where(x => x.ProductCode.HasValue).Select(x => x.ProductCode.Value).Distinct().ToArray();
        var mappings = await db.TaxProductMappings.AsNoTracking().Where(x => x.IsDefault && ids.Contains(x.ProductId)).ToListAsync(ct);
        var memory = await db.TaxMemories.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, ct);
        await tx.CommitAsync(ct);
        return new(factor, memory, mappings);
    }

    public async Task SetMappingAsync(Guid productId, int unit, string goodsId, string taxUnit, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Products.AnyAsync(x => x.Id == productId && !x.IsDeleted, ct)) throw new KeyNotFoundException();
        var group = db.TaxProductMappings.Where(x => x.ProductId == productId && x.Unit == unit);
        await group.Where(x => x.IsDefault && x.GoodsId != goodsId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), ct);
        var mapping = await group.SingleOrDefaultAsync(x => x.GoodsId == goodsId, ct);
        if (mapping is null)
        {
            mapping = new TaxProductMapping { Id = Guid.NewGuid(), ProductId = productId, Unit = unit, GoodsId = goodsId };
            db.TaxProductMappings.Add(mapping);
        }
        mapping.TaxUnit = taxUnit;
        mapping.IsDefault = true;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<string> ReadSnapshotAsync(Guid id, CancellationToken ct)
    {
        var payload = await db.TaxInvoiceSnapshots.AsNoTracking().Where(x => x.Id == id)
            .Select(x => x.ProtectedPreview).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
        return protection.Unprotect(payload);
    }

    public async Task<Guid> SaveSnapshotAsync(Guid factorId, Guid memoryId, string preview, string actorId, CancellationToken ct)
    {
        var snapshot = new TaxInvoiceSnapshot
        {
            Id = Guid.NewGuid(), FactorId = factorId, TaxMemoryId = memoryId,
            CreatedUtc = DateTime.UtcNow, CreatedBy = actorId,
            ProtectedPreview = protection.Protect(preview)
        };
        db.TaxInvoiceSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
        return snapshot.Id;
    }
}
