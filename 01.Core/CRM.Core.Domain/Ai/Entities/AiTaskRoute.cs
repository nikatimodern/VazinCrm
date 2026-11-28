using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Ai.Entities;

public sealed class AiTaskRoute : BaseEntity
{
    private AiTaskRoute() { }

    public AiTaskRoute(string taskKey, Guid primaryModelId, int timeoutMs, int maxOutputTokens)
    {
        Id = Guid.NewGuid();
        TaskKey = AiProvider.Required(taskKey, 96, nameof(taskKey));
        Configure(primaryModelId, timeoutMs, maxOutputTokens);
    }

    public string TaskKey { get; private set; }
    public Guid PrimaryModelId { get; private set; }
    public int TimeoutMs { get; private set; }
    public int MaxOutputTokens { get; private set; }
    public bool IsEnabled { get; private set; }

    public void Configure(Guid primaryModelId, int timeoutMs, int maxOutputTokens)
    {
        if (primaryModelId == Guid.Empty) throw new ArgumentException("Model is required.", nameof(primaryModelId));
        if (timeoutMs is < 1 or > 300000) throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        if (maxOutputTokens is < 1 or > 8192) throw new ArgumentOutOfRangeException(nameof(maxOutputTokens));
        PrimaryModelId = primaryModelId;
        TimeoutMs = timeoutMs;
        MaxOutputTokens = maxOutputTokens;
        IsEnabled = false;
    }

    // No fallback or runtime activation before the data-policy gate is ready.
    public void Disable() => IsEnabled = false;
}
