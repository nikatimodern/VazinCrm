namespace CRM.Core.ApplicationServices.Ai.Abstractions;

public static class AiProviderPolicy
{
    public static bool AllowsData(AiDataClass dataClass, bool isLocal) => dataClass switch
    {
        AiDataClass.Synthetic or AiDataClass.ApprovedDeidentified => true,
        AiDataClass.LocalOnly => isLocal,
        _ => false
    };

    public static bool SupportsRequest(IAiProvider provider, AiProviderRequest request)
    {
        var required = AiProviderCapabilities.Text;
        if (request.RequireJsonObject)
            required |= AiProviderCapabilities.StructuredJson;
        return (provider.Capabilities & required) == required;
    }

    public static bool IsValid(AiProviderRequest request)
    {
        if (request is null || request.RequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.ModelKey) || request.ModelKey.Length > 128 ||
            request.Messages is null || request.Messages.Count is < 1 or > 64 ||
            request.MaxOutputTokens is < 1 or > 8192 ||
            request.Timeout <= TimeSpan.Zero || request.Timeout > TimeSpan.FromMinutes(5))
            return false;

        long total = 0;
        foreach (var message in request.Messages)
        {
            if (message is null || !Enum.IsDefined(message.Role) ||
                string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 32768)
                return false;
            total += message.Content.Length;
            if (total > 131072) return false;
        }
        return request.Messages.Any(message => message.Role == AiMessageRole.User);
    }
}
