using System.Data;
using CRM.Core.Domain.Taxation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed partial class TaxationRepository(CRMContextDb db, ITaxPreviewProtector protection) : ITaxationRepository
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
        await LockAsync(db, "tax-memory-activation", ct);
        if (!await db.TaxMemories.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException();
        var now = DateTime.UtcNow;
        await db.TaxMemories.Where(x => x.IsActive && x.Id != id).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedUtc, now), ct);
        await db.TaxMemories.Where(x => x.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsActive, true).SetProperty(x => x.UpdatedUtc, now), ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(db, "tax-memory-activation", ct);
        if (!await db.TaxMemories.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException();
        await db.TaxMemories.Where(x => x.Id == id && x.IsActive).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsActive, false).SetProperty(x => x.UpdatedUtc, DateTime.UtcNow), ct);
        await tx.CommitAsync(ct);
    }

    public async Task<TaxPreviewSource> GetPreviewSourceAsync(Guid factorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var source = await ReadSourceAsync(factorId, ct);
        await tx.CommitAsync(ct);
        return source;
    }

    private async Task<TaxPreviewSource> ReadSourceAsync(Guid factorId, CancellationToken ct, Guid? memoryId = null)
    {
        var factor = await db.Factors.AsNoTracking().Include(x => x.FactorItems)
            .SingleOrDefaultAsync(x => x.Id == factorId && !x.IsDeleted, ct) ?? throw new KeyNotFoundException();
        var ids = factor.FactorItems.Where(x => x.ProductCode.HasValue).Select(x => x.ProductCode.Value).Distinct().ToArray();
        var mappings = await db.TaxProductMappings.AsNoTracking().Where(x => x.IsDefault && ids.Contains(x.ProductId)).ToListAsync(ct);
        var memory = await db.TaxMemories.AsNoTracking().SingleOrDefaultAsync(x => memoryId.HasValue ? x.Id == memoryId : x.IsActive, ct);
        var afterDiscount = await db.ASettings.AsNoTracking().Select(x => (bool?)x.TaxAfterDiscount).FirstOrDefaultAsync(ct);
        return new(factor, memory, mappings, afterDiscount);
    }

    public async Task SetMappingAsync(Guid productId, int unit, string goodsId, string taxUnit, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(db, $"tax-product-mapping:{productId:D}:{unit}", ct);
        if (!await db.Products.AnyAsync(x => x.Id == productId && !x.IsDeleted, ct)) throw new KeyNotFoundException();
        var group = db.TaxProductMappings.Where(x => x.ProductId == productId && x.Unit == unit);
        if (!await group.AnyAsync(ct) &&
            !await db.ProductAmounts.AnyAsync(x => x.ProductId == productId && x.Unit == unit && !x.IsDeleted, ct) &&
            !await db.FactorItems.AnyAsync(x => x.ProductCode == productId && x.Unit == unit && !x.IsDeleted, ct))
            throw new ArgumentException("واحد انتخابی برای این محصول در ERP ثبت نشده است.");
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

    public async Task<Guid> SaveSnapshotAsync(Guid factorId, string actorId, Func<TaxPreviewSource, string> validate, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var snapshot = CreateSnapshot(await ReadSourceAsync(factorId, ct), actorId, validate);
        db.TaxInvoiceSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return snapshot.Id;
    }

    public async Task<Guid> EnqueueAsync(Guid factorId, string actorId, Func<TaxPreviewSource, string> validate, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(db, $"tax-dispatch:{factorId:D}", ct);
        var existing = await db.TaxDispatches.AsNoTracking().Where(x => x.FactorId == factorId && x.Kind == TaxDispatchKind.Original)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (existing.HasValue)
        {
            await tx.CommitAsync(ct);
            return existing.Value;
        }

        if (await db.TaxDispatches.AnyAsync(x => x.FactorId == factorId &&
            (x.Kind == TaxDispatchKind.Correction || x.Kind == TaxDispatchKind.Return), ct))
            throw new ArgumentException("این فاکتور قبلاً به عنوان سند ارجاعی ثبت شده است.");
        var source = await ReadSourceAsync(factorId, ct);
        if (source.Factor.status != CRM.Core.Domain.Factors.Enum.FactorStatus.تایید_شده)
            throw new ArgumentException("فقط فاکتور تاییدشده وارد صف می‌شود.");

        var snapshot = CreateSnapshot(source, actorId, validate);
        var entry = CreateDispatch(snapshot, source.ActiveMemory, TaxDispatchKind.Original);
        db.TaxInvoiceSnapshots.Add(snapshot);
        db.TaxDispatches.Add(entry);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return entry.Id;
    }

    private TaxInvoiceSnapshot CreateSnapshot(TaxPreviewSource source, string actorId, Func<TaxPreviewSource, string> validate)
    {
        var preview = validate(source);
        return new TaxInvoiceSnapshot
        {
            Id = Guid.NewGuid(), FactorId = source.Factor.Id, TaxMemoryId = source.ActiveMemory.Id,
            CreatedUtc = DateTime.UtcNow, CreatedBy = actorId,
            ProtectedPreview = protection.Protect(preview)
        };
    }

    private static TaxDispatch CreateDispatch(TaxInvoiceSnapshot snapshot, TaxMemory memory, TaxDispatchKind kind) =>
        new()
        {
            Id = Guid.NewGuid(), FactorId = snapshot.FactorId, SnapshotId = snapshot.Id,
            MemoryId = memory.Id, FiscalId = memory.FiscalId, MemoryTitle = memory.Title,
            MemoryUpdatedUtc = memory.UpdatedUtc, MemoryWasActive = memory.IsActive,
            Kind = kind, QueuedUtc = snapshot.CreatedUtc, Status = TaxDispatchStatus.Pending
        };

    internal static Task LockAsync(CRMContextDb context, string resource, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51000, 'Could not acquire the taxation transaction lock.', 1;
            """, ct);
}
