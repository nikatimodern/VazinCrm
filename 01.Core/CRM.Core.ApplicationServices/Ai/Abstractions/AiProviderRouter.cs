namespace CRM.Core.ApplicationServices.Ai.Abstractions;

// Internal route selected by trusted application configuration, never by a
// model or HTTP request. This first slice intentionally has no automatic fallback.
public sealed record AiProviderRoute(string ProviderKey, string ModelKey, bool Enabled);

public sealed class AiProviderRouter
{
    private readonly IReadOnlyDictionary<string, IAiProvider> providers;

    public AiProviderRouter(IEnumerable<IAiProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        this.providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.Ordinal);
    }

    public async Task<AiProviderResult> GenerateAsync(
        AiProviderRoute route, AiProviderRequest request, CancellationToken cancellationToken)
    {
        if (route is null || request is null || !route.Enabled ||
            string.IsNullOrWhiteSpace(route.ProviderKey) || string.IsNullOrWhiteSpace(route.ModelKey))
            return new(AiProviderStatus.ConfigurationError, "", "", ErrorCode: "route_disabled_or_missing");

        AiProviderResult Fail(AiProviderStatus status, string code) =>
            new(status, route.ProviderKey, route.ModelKey, ErrorCode: code);

        if (cancellationToken.IsCancellationRequested)
            return Fail(AiProviderStatus.Cancelled, "cancelled");
        if (!providers.TryGetValue(route.ProviderKey, out var provider))
            return Fail(AiProviderStatus.ConfigurationError, "provider_not_registered");

        // The model comes exclusively from the configured route.
        var routed = request with { ModelKey = route.ModelKey };
        if (!AiProviderPolicy.IsValid(routed))
            return Fail(AiProviderStatus.Rejected, "invalid_request");
        if (!AiProviderPolicy.AllowsData(routed.DataClass, provider.IsLocal))
            return Fail(AiProviderStatus.Rejected, "data_policy");
        if (!AiProviderPolicy.SupportsRequest(provider, routed))
            return Fail(AiProviderStatus.Unsupported, "unsupported_capability");

        return await provider.GenerateAsync(routed, cancellationToken);
    }
}
