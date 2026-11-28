using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Taxation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed partial class TaxationRepository
{
    public async Task<Guid> EnqueueReferenceAsync(Guid referenceId, Guid? factorId, TaxDispatchKind kind, string actorId,
        Func<TaxReferenceSource, CancellationToken, Task<TaxReferenceRegistration>> register, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(db, $"tax-reference:{referenceId:D}", ct);
        if (factorId.HasValue)
            await LockAsync(db, $"tax-dispatch:{factorId:D}", ct);
        var existing = await db.TaxDispatches.AsNoTracking()
            .Where(x => x.Kind == kind && x.ReferencedDispatchId == referenceId && (factorId == null || x.FactorId == factorId))
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (existing.HasValue)
        {
            await tx.CommitAsync(ct);
            return existing.Value;
        }

        var reference = await db.TaxDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == referenceId, ct)
            ?? throw new KeyNotFoundException();
        if (reference.Kind == TaxDispatchKind.Cancellation || reference.Status != TaxDispatchStatus.Accepted ||
            string.IsNullOrEmpty(reference.ProtectedSubmission) || string.IsNullOrEmpty(reference.ProtectedWorkFolderEvidence))
            throw new ArgumentException("سند مرجع پذیرفته‌شده و وضعیت معتبر کارپوشه لازم است.");
        if (await db.TaxDispatches.AnyAsync(x => x.ReferencedDispatchId == referenceId &&
                (x.Kind == TaxDispatchKind.Cancellation || x.Status != TaxDispatchStatus.Rejected), ct))
            throw new ArgumentException("مرجع قبلاً سند ابطالی یا ارجاعی تعیین‌تکلیف‌نشده یا پذیرفته‌شده دارد.");

        var snapshotSource = await db.TaxInvoiceSnapshots.AsNoTracking().SingleAsync(x => x.Id == reference.SnapshotId &&
            x.FactorId == reference.FactorId && x.TaxMemoryId == reference.MemoryId, ct);
        var rootFactor = await ReadRootFactorAsync(reference, ct);
        var candidate = await ReadCandidateAsync(factorId, kind, rootFactor, reference.MemoryId, ct);
        var memory = candidate?.ActiveMemory ?? await db.TaxMemories.AsNoTracking()
            .SingleAsync(x => x.Id == reference.MemoryId, ct);
        if (memory.FiscalId != reference.FiscalId)
            throw new ArgumentException("شناسهٔ حافظه با سند مرجع یکسان نیست.");

        var registered = await register(new(reference, protection.Unprotect(snapshotSource.ProtectedPreview),
            protection.Unprotect(reference.ProtectedSubmission),
            protection.Unprotect(reference.ProtectedWorkFolderEvidence), candidate), ct);
        var snapshot = new TaxInvoiceSnapshot
        {
            Id = Guid.NewGuid(), FactorId = factorId ?? reference.FactorId, TaxMemoryId = memory.Id,
            CreatedUtc = DateTime.UtcNow, CreatedBy = actorId, ProtectedPreview = protection.Protect(registered.Preview)
        };
        var entry = CreateDispatch(snapshot, memory, kind);
        entry.ReferencedDispatchId = referenceId;
        entry.AuthorityEnvironment = registered.Binding.Environment;
        entry.CertificateSha256 = registered.Binding.CertificateSha256;
        entry.AuthorityProtocol = registered.Binding.Protocol;
        entry.InvoiceSerial = registered.Serial;
        entry.TaxId = registered.TaxId;
        entry.ProtectedSubmission = protection.Protect(registered.Payload);
        db.TaxInvoiceSnapshots.Add(snapshot);
        db.TaxDispatches.Add(entry);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ArgumentException("شماره سریال یا سند ارجاعی قبلاً ثبت شده است.");
        }
        return entry.Id;
    }

    private async Task<TaxPreviewSource> ReadCandidateAsync(Guid? factorId, TaxDispatchKind kind, Guid rootFactor,
        Guid memoryId, CancellationToken ct)
    {
        if (!factorId.HasValue)
            return null;
        if (factorId == rootFactor || await db.TaxDispatches.AnyAsync(x => x.FactorId == factorId, ct))
            throw new ArgumentException("فاکتور مستقل و ثبت‌نشده برای سند ارجاعی لازم است.");
        var source = await ReadSourceAsync(factorId.Value, ct, memoryId);
        if (source.Factor.status != FactorStatus.تایید_شده ||
            (kind == TaxDispatchKind.Return ? source.Factor.ParentReturnedId != rootFactor : source.Factor.ParentReturnedId.HasValue))
            throw new ArgumentException("فاکتور ارجاعی باید تأیید شده و ارتباط آن با فاکتور اصلی معتبر باشد.");
        return source;
    }

    private async Task<Guid> ReadRootFactorAsync(TaxDispatch entry, CancellationToken ct)
    {
        var visited = new HashSet<Guid> { entry.Id };
        while (entry.ReferencedDispatchId is { } parent)
        {
            if (!visited.Add(parent))
                throw new ArgumentException("زنجیرهٔ ارجاع مالیاتی معتبر نیست.");
            entry = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == parent, ct);
        }
        if (entry.Kind != TaxDispatchKind.Original)
            throw new ArgumentException("صورتحساب اصلی در زنجیرهٔ مرجع یافت نشد.");
        return entry.FactorId;
    }
}
