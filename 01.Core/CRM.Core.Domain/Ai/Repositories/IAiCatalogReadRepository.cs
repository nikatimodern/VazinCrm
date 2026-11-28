using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Ai.Repositories;

public sealed record AiProviderSummary(Guid Id, string Key, string DisplayName, bool IsEnabled);
public sealed record AiModelSummary(Guid Id, Guid ProviderId, string ModelKey, string DisplayName, bool IsEnabled, DateTimeOffset? VerifiedAtUtc);
public sealed record AiRouteSummary(string TaskKey, Guid PrimaryModelId, int TimeoutMs, int MaxOutputTokens, bool IsEnabled);

// Administrative metadata only. Callers must enforce catalog permission before any read.
// Endpoint and secret references are deliberately absent from these projections.
public interface IAiCatalogReadRepository
{
    Task<IReadOnlyList<AiProviderSummary>> GetProvidersAsync(int offset, int limit, CancellationToken ct);
    Task<IReadOnlyList<AiModelSummary>> GetModelsAsync(Guid providerId, int offset, int limit, CancellationToken ct);
    Task<AiRouteSummary> GetRouteAsync(string taskKey, CancellationToken ct);
}
