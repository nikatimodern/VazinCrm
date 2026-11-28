using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Ai.Entities;

public sealed class AiModel : BaseEntity
{
    private AiModel() { }

    public AiModel(Guid providerId, string modelKey, string displayName)
    {
        if (providerId == Guid.Empty) throw new ArgumentException("Provider is required.", nameof(providerId));
        Id = Guid.NewGuid();
        ProviderId = providerId;
        ModelKey = AiProvider.Required(modelKey, 128, nameof(modelKey));
        DisplayName = AiProvider.Required(displayName, 128, nameof(displayName));
    }

    public Guid ProviderId { get; private set; }
    public string ModelKey { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsEnabled { get; private set; }

    // Null means not verified; do not infer live capabilities from a model name.
    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public void Disable() => IsEnabled = false;
}
