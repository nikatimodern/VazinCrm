namespace CRM.Core.ApplicationServices.Ai.Abstractions;

// Provider boundary only. Authorization, data classification and routing must be
// resolved by trusted application use cases, never from a browser or model output.
[Flags]
public enum AiProviderCapabilities
{
    None = 0, Text = 1, StructuredJson = 2, Image = 4, Pdf = 8,
    AudioInput = 16, ToolCalling = 32, Streaming = 64, UsageReporting = 128
}

public enum AiDataClass { Unclassified, Synthetic, ApprovedDeidentified, LocalOnly }
public enum AiMessageRole { System, User, Assistant }
public enum AiProviderStatus
{
    Succeeded, Rejected, Unsupported, ConfigurationError, RateLimited,
    Unavailable, Timeout, Cancelled, InvalidResponse
}

public sealed record AiProviderMessage(AiMessageRole Role, string Content);
public sealed record AiProviderRequest(
    Guid RequestId,
    string ModelKey,
    IReadOnlyList<AiProviderMessage> Messages,
    AiDataClass DataClass,
    bool RequireJsonObject,
    int MaxOutputTokens,
    TimeSpan Timeout);

public sealed record AiUsage(long? InputTokens, long? OutputTokens, long? TotalTokens);
public sealed record AiProviderResult(
    AiProviderStatus Status, string ProviderKey, string ModelKey,
    string? Content = null, string? FinishReason = null,
    AiUsage? Usage = null, string? ErrorCode = null)
{
    public bool IsSuccess => Status == AiProviderStatus.Succeeded;
}

public interface IAiProvider
{
    string ProviderKey { get; }
    bool IsLocal { get; }
    AiProviderCapabilities Capabilities { get; }
    Task<AiProviderResult> GenerateAsync(AiProviderRequest request, CancellationToken cancellationToken);
}
