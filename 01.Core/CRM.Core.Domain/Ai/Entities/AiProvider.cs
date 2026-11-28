using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Ai.Entities;

public enum AiProviderKind { Cloud = 1, Local = 2 }

public sealed class AiProvider : BaseEntity
{
    private AiProvider() { }

    public AiProvider(string key, string displayName, AiProviderKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Id = Guid.NewGuid();
        Key = Required(key, 64, nameof(key));
        DisplayName = Required(displayName, 128, nameof(displayName));
        Kind = kind;
    }

    public string Key { get; private set; }
    public string DisplayName { get; private set; }
    public AiProviderKind Kind { get; private set; }
    public bool IsEnabled { get; private set; }

    // Runtime configuration identifiers, not URLs, credentials or API keys.
    public string EndpointRef { get; private set; }
    public string SecretRef { get; private set; }

    public void Configure(string endpointRef, string secretRef)
    {
        var endpoint = Required(endpointRef, 128, nameof(endpointRef));
        var secret = secretRef is null ? null : Required(secretRef, 128, nameof(secretRef));
        EndpointRef = endpoint;
        SecretRef = secret;
        IsEnabled = false;
    }

    // Enabling is intentionally deferred until secure configuration and
    // health verification are implemented in the application layer.
    public void Disable() => IsEnabled = false;

    internal static string Required(string value, int maxLength, string parameter)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > maxLength)
            throw new ArgumentException("Required value is invalid.", parameter);
        return text;
    }
}
