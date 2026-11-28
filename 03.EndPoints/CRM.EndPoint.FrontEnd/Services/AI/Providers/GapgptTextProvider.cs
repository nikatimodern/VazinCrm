using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Ai.Abstractions;

namespace CRM.EndPoint.FrontEnd.Services.AI.Providers;

// Intentionally not registered or exposed until secure configuration, catalog
// authorization and synthetic API verification are complete. Supply a dedicated
// HttpClient with redirects disabled and credentials from secure configuration.
public sealed class GapgptTextProvider(HttpClient client) : IAiProvider
{
    private const int MaxResponseBytes = 1024 * 1024;
    public string ProviderKey => "gapgpt";
    public bool IsLocal => false;
    // Implemented formats, not a claim that every remote model supports them.
    public AiProviderCapabilities Capabilities =>
        AiProviderCapabilities.Text | AiProviderCapabilities.StructuredJson;

    public async Task<AiProviderResult> GenerateAsync(AiProviderRequest request, CancellationToken cancellationToken)
    {
        AiProviderResult Fail(AiProviderStatus status, string code) =>
            new(status, ProviderKey, request?.ModelKey ?? "", ErrorCode: code);

        if (cancellationToken.IsCancellationRequested)
            return Fail(AiProviderStatus.Cancelled, "cancelled");
        if (!AiProviderPolicy.IsValid(request))
            return Fail(AiProviderStatus.Rejected, "invalid_request");
        if (!AiProviderPolicy.AllowsData(request.DataClass, IsLocal))
            return Fail(AiProviderStatus.Rejected, "data_policy");
        if (!AiProviderPolicy.SupportsRequest(this, request))
            return Fail(AiProviderStatus.Unsupported, "unsupported_capability");
        if (client.BaseAddress is not { IsAbsoluteUri: true } address ||
            address.Scheme != Uri.UriSchemeHttps || address.UserInfo.Length != 0 ||
            address.Query.Length != 0 || address.Fragment.Length != 0 ||
            !address.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            return Fail(AiProviderStatus.ConfigurationError, "invalid_endpoint");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout);
        try
        {
            var messages = request.Messages.Select(message => new
            {
                role = message.Role.ToString().ToLowerInvariant(),
                content = message.Content
            }).ToArray();
            object payload = request.RequireJsonObject
                ? new { model = request.ModelKey, messages, max_tokens = request.MaxOutputTokens,
                    response_format = new { type = "json_object" } }
                : new { model = request.ModelKey, messages, max_tokens = request.MaxOutputTokens };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(payload)
            };
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return Fail(response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => AiProviderStatus.RateLimited,
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AiProviderStatus.ConfigurationError,
                    HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => AiProviderStatus.Rejected,
                    _ => AiProviderStatus.Unavailable
                }, "provider_http_" + (int)response.StatusCode);

            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                return Fail(AiProviderStatus.InvalidResponse, "response_too_large");
            using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk.AsMemory(), timeout.Token)) != 0)
            {
                if (buffer.Length + read > MaxResponseBytes)
                    return Fail(AiProviderStatus.InvalidResponse, "response_too_large");
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            using var json = await JsonDocument.ParseAsync(buffer, cancellationToken: timeout.Token);
            return ParseResult(json.RootElement, request);
        }
        catch (OperationCanceledException)
        {
            return cancellationToken.IsCancellationRequested
                ? Fail(AiProviderStatus.Cancelled, "cancelled")
                : Fail(AiProviderStatus.Timeout, "timeout");
        }
        catch (HttpRequestException)
        {
            return Fail(AiProviderStatus.Unavailable, "transport_error");
        }
        catch (IOException)
        {
            return Fail(AiProviderStatus.Unavailable, "response_io_error");
        }
        catch (JsonException)
        {
            return Fail(AiProviderStatus.InvalidResponse, "invalid_json");
        }
    }

    private AiProviderResult ParseResult(JsonElement root, AiProviderRequest request)
    {
        AiProviderResult Invalid(string code) =>
            new(AiProviderStatus.InvalidResponse, ProviderKey, request.ModelKey, ErrorCode: code);
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0 ||
            choices[0].ValueKind != JsonValueKind.Object ||
            !choices[0].TryGetProperty("message", out var message) ||
            message.ValueKind != JsonValueKind.Object ||
            !message.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(content.GetString()))
            return Invalid("missing_content");

        var text = content.GetString();
        var finish = ReadString(choices[0], "finish_reason");
        if (finish is "length" or "content_filter" or "tool_calls")
            return Invalid("incomplete_response");
        if (request.RequireJsonObject)
        {
            using var structured = JsonDocument.Parse(text);
            if (structured.RootElement.ValueKind != JsonValueKind.Object)
                return Invalid("expected_json_object");
        }
        AiUsage usage = null;
        if (root.TryGetProperty("usage", out var info) && info.ValueKind == JsonValueKind.Object)
            usage = new(ReadCount(info, "prompt_tokens"), ReadCount(info, "completion_tokens"), ReadCount(info, "total_tokens"));
        return new(AiProviderStatus.Succeeded, ProviderKey, request.ModelKey, text, finish, usage);
    }

    private static string ReadString(JsonElement value, string key) =>
        value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;

    private static long? ReadCount(JsonElement value, string key) =>
        value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.Number &&
        item.TryGetInt64(out var count) && count >= 0 ? count : null;
}
