using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed partial class TaxationRepository
{
    public Task<TaxMemory> FindMemoryAsync(Guid id, CancellationToken ct) =>
        db.TaxMemories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<TaxMemory> FindActiveMemoryAsync(CancellationToken ct) =>
        db.TaxMemories.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, ct);

    public Task<bool> HasEnabledSandboxAsync(CancellationToken ct) =>
        db.TaxMemories.AnyAsync(x => x.Environment == TaxAuthorityEnvironment.Sandbox && x.SandboxEnabled, ct);

    public async Task<Guid> SaveSettingsAsync(TaxMemorySettings settings, string actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(db, "tax-memory-activation", ct);
        var current = settings.Id is { } id
            ? await db.TaxMemories.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException()
            : new TaxMemory { Id = Guid.NewGuid(), FiscalId = settings.FiscalId, Environment = settings.Environment };
        if (settings.Id.HasValue && current.UpdatedUtc != settings.ExpectedUpdatedUtc)
            throw new TaxPreviewConflictException("تنظیمات حافظه تغییر کرده است. صفحه را دوباره بارگذاری کنید.");
        if ((settings.Environment != current.Environment || settings.FiscalId != current.FiscalId) &&
            (current.IsActive || await db.TaxInvoiceSnapshots.AnyAsync(x => x.TaxMemoryId == current.Id, ct) ||
             await db.TaxDispatches.AnyAsync(x => x.MemoryId == current.Id, ct)))
            throw new ArgumentException("محیط یا شناسه حافظه فعال یا دارای سند قابل تغییر نیست. حافظه مستقل ثبت کنید.");
        if ((current.Protocol != settings.Protocol || current.SellerEconomicCode is not null &&
             current.SellerEconomicCode != settings.SellerEconomicCode) && await HasPendingForMemoryAsync(current.Id, ct))
            throw new ArgumentException("روش ارسال و شناسه اقتصادی تا تعیین تکلیف اسناد باز این حافظه قابل تغییر نیستند.");

        var now = DateTime.UtcNow;
        if (!settings.Id.HasValue)
            db.TaxMemories.Add(current);
        var updatedKey = current.CredentialFingerprint != settings.CredentialFingerprint;
        current.Protocol = settings.Protocol;
        current.Environment = settings.Environment;
        current.FiscalId = settings.FiscalId;
        current.Title = settings.Title;
        current.SellerEconomicCode = settings.SellerEconomicCode;
        current.CredentialFingerprint = settings.CredentialFingerprint;
        current.SandboxEnabled = settings.SandboxEnabled;
        current.UpdatedUtc = now;
        db.TaxMemoryAuditEvents.Add(new() { MemoryId = current.Id, ActorId = actorId,
            Operation = !settings.Id.HasValue ? "Created" : updatedKey ? "SettingsAndCredentialChanged" : "SettingsChanged",
            OccurredUtc = now });
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            throw new ArgumentException("شناسه حافظه قبلاً در این محیط ثبت شده است.", ex);
        }
        return current.Id;
    }

    private Task<bool> HasPendingForMemoryAsync(Guid memoryId, CancellationToken ct) =>
        db.TaxDispatches.AnyAsync(x => x.MemoryId == memoryId &&
            (x.Status == TaxDispatchStatus.Pending || x.Status == TaxDispatchStatus.InFlight ||
             x.Status == TaxDispatchStatus.Submitted || x.Status == TaxDispatchStatus.Unknown), ct);
}
