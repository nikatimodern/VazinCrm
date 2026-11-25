using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public sealed record TaxMemorySettings(Guid? Id, string Title, string FiscalId, TaxAuthorityEnvironment Environment,
    TaxAuthorityProtocol Protocol, string SellerEconomicCode, string CredentialFingerprint, bool SandboxEnabled, DateTime? ExpectedUpdatedUtc);

public sealed class TaxMemoryAuditEvent
{
    public long Id { get; set; }
    public Guid MemoryId { get; set; }
    public string ActorId { get; set; }
    public string Operation { get; set; }
    public DateTime OccurredUtc { get; set; }
}

public interface ITaxMemorySettingsRepository
{
    Task<TaxMemory> FindMemoryAsync(Guid id, CancellationToken ct);
    Task<TaxMemory> FindActiveMemoryAsync(CancellationToken ct);
    Task<bool> HasEnabledSandboxAsync(CancellationToken ct);
    Task<Guid> SaveSettingsAsync(TaxMemorySettings settings, string actorId, CancellationToken ct);
}
