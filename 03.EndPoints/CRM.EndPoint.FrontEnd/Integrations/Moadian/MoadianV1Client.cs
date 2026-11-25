using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class MoadianV1Client(HttpClient http, ITaxV1SigningKeys keys, TimeProvider clock) : ITaxAuthorityClient
{
    private const string Sync = "api/self-tsp/sync/";
    private const string Enqueue = "api/self-tsp/async/normal-enqueue";

    public async Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct)
    {
        if (submission.Environment != TaxAuthorityEnvironment.Sandbox || submission.Protocol != TaxAuthorityProtocol.V1 ||
            submission.Kind != TaxDispatchKind.Original)
            throw new ArgumentException("در نسخه ۱ فقط ارسال صورتحساب اصلی آزمایشی فعال است.");

        using var invoice = JsonDocument.Parse(submission.Payload);
        var header = Property(invoice.RootElement, "header");
        if (Text(header, "taxid") != submission.TaxId || !TaxInvoicePayload.MatchesTaxId(submission.FiscalId, submission.TaxId, submission.InvoiceSerial) ||
            Number(header, "inty") != 1 || Number(header, "inp") != 1)
            throw new ArgumentException("نسخه ۱ فقط برای فاکتور اصلی فروش داخلی نوع اول فعال است.");

        using var privateKey = keys.Open(submission.V1KeyFingerprint);
        var token = await GetTokenAsync(submission.FiscalId, privateKey, ct);
        using var info = await SyncAsync("GET_SERVER_INFORMATION", new Packet(null, "GET_SERVER_INFORMATION", null),
            null, null, ct);
        var configured = Property(GetResultData(info.RootElement, "SERVER_INFORMATION"), "publicKeys");
        if (configured.ValueKind != JsonValueKind.Array) throw InvalidResponse();
        var candidates = configured.EnumerateArray().Where(key => Text(key, "algorithm") == "RSA" &&
            Number(key, "purpose") == 1).Take(2).ToArray();
        if (candidates.Length != 1 || Text(candidates[0], "id") is not { Length: > 0 } keyId ||
            Text(candidates[0], "key") is not { Length: > 0 } publicKey)
            throw InvalidResponse();

        using var authorityKey = RSA.Create();
        var encoded = Convert.FromBase64String(publicKey);
        authorityKey.ImportSubjectPublicKeyInfo(encoded, out var consumed);
        if (consumed != encoded.Length || authorityKey.KeySize < 2048) throw InvalidResponse();

        var encrypted = TaxV1Cryptography.Encrypt(submission.Payload, authorityKey);
        var packet = new Packet(attemptId, "INVOICE.V01", encrypted.Data, keyId, encrypted.SymmetricKey, encrypted.Iv,
            submission.FiscalId, TaxV1Cryptography.Sign(invoice.RootElement, privateKey));
        return new PreparedTaxRequest(attemptId, Build(Enqueue, new[] { packet }, true, token, privateKey, attemptId), TaxAuthorityProtocol.V1);
    }

    public async Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct)
    {
        using var response = await ExchangeAsync(request.Message, ct);
        var entries = Property(response.RootElement, "result");
        var entry = Match(entries, x => Id(x, "uid") == request.AttemptId);
        if (entry is not { } receipt || Id(receipt, "referenceNumber") is not { } reference ||
            Text(receipt, "errorCode") is { Length: > 0 })
            throw InvalidResponse();

        return new TaxAcknowledgement(reference, receipt.GetRawText());
    }

    public async Task<TaxProcessingResult> InquireAsync(TaxSubmission submission, CancellationToken ct)
    {
        if (submission.Environment != TaxAuthorityEnvironment.Sandbox || submission.Protocol != TaxAuthorityProtocol.V1 ||
            submission.AttemptId is not { } attempt)
            throw new ArgumentException("شناسه ارسال نسخه ۱ معتبر نیست.");
        using var key = keys.Open(submission.V1KeyFingerprint);
        var token = await GetTokenAsync(submission.FiscalId, key, ct);
        var operation = submission.ReferenceNumber.HasValue ? "INQUIRY_BY_REFERENCE_NUMBER" : "INQUIRY_BY_UID";
        object data = submission.ReferenceNumber is { } reference
            ? new { referenceNumber = new[] { reference } }
            : new[] { new { uid = attempt, fiscalId = submission.FiscalId } };
        using var result = await SyncAsync(operation, new Packet(null, operation, data, FiscalId: submission.FiscalId),
            token, key, ct);
        var entry = Match(GetResultData(result.RootElement, "INQUIRY_RESULT"),
            x => Id(x, "uid") == attempt && (Text(x, "fiscalId") == submission.FiscalId ||
                (Text(x, "status") == "FAILED" && Text(x, "fiscalId") is null)));
        if (entry is not { } row) return null;
        var status = Text(row, "status");
        var matchedReference = Id(row, "referenceNumber");
        if (status == "NOT_FOUND") return null;
        if (submission.ReferenceNumber.HasValue && matchedReference != submission.ReferenceNumber)
            throw InvalidResponse();
        if (status == "SUCCESS" && (matchedReference is null || Text(Property(row, "data"), "taxResult") != "SUCCESS"))
            throw InvalidResponse();
        if (status is not ("SUCCESS" or "FAILED" or "IN_PROGRESS" or "TIMEOUT"))
            throw InvalidResponse();
        return new TaxProcessingResult(matchedReference, status, row.GetRawText());
    }

    public Task<TaxWorkFolderResult> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct) =>
        throw new ArgumentException("استعلام مستقل وضعیت کارپوشه با نسخه ۱ در ERP فعال نیست.");

    internal async Task VerifyIdentityAsync(string fiscalId, TaxSigningBinding binding, CancellationToken ct)
    {
        using var key = keys.Open(binding.V1KeyFingerprint);
        var token = await GetTokenAsync(fiscalId, key, ct);
        using var response = await SyncAsync("GET_FISCAL_INFORMATION",
            new Packet(null, "GET_FISCAL_INFORMATION", null, FiscalId: fiscalId), token, key, ct);
        var info = GetResultData(response.RootElement, "FISCAL_INFORMATION");
        if (Text(info, "fiscalStatus") != "ACTIVE" || Text(info, "economicCode") != binding.SellerEconomicCode)
            throw new ArgumentException("حافظه مالیاتی فعال نیست یا شناسه اقتصادی فروشنده با پاسخ سازمان همخوانی ندارد.");
    }

    internal async Task<string> GetTokenAsync(string fiscalId, RSA key, CancellationToken ct)
    {
        using var result = await SyncAsync("GET_TOKEN", new Packet(null, "GET_TOKEN", new { username = fiscalId }),
            null, key, ct);
        return Text(GetResultData(result.RootElement, "TOKEN_RESULT"), "token") is { Length: > 0 } token
            ? token : throw InvalidResponse();
    }

    private async Task<JsonDocument> SyncAsync(string operation, Packet packet, string token, RSA key, CancellationToken ct)
    {
        using var request = Build(Sync + operation, packet, false, token, key);
        return await ExchangeAsync(request, ct);
    }

    private HttpRequestMessage Build(string path, object packet, bool batch, string token, RSA key, Guid? traceId = null)
    {
        var trace = (traceId ?? Guid.NewGuid()).ToString("D");
        var time = clock.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var scope = new System.Collections.Generic.Dictionary<string, object>
        {
            [batch ? "packets" : "packet"] = packet,
            ["requestTraceId"] = trace,
            ["timestamp"] = time
        };
        if (token is not null) scope["Authorization"] = token;
        var signature = key is null ? null : TaxV1Cryptography.Sign(JsonSerializer.SerializeToElement(scope, JsonSerializerOptions.Web), key);
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = batch
                ? JsonContent.Create(new { packets = packet, signature, signatureKeyId = (string)null })
                : JsonContent.Create(new { packet, signature })
        };
        message.Headers.Add("requestTraceId", trace);
        message.Headers.Add("timestamp", time);
        if (token is not null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return message;
    }

    private async Task<JsonDocument> ExchangeAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new TaxTransportException($"Tax V1 authority returned HTTP {(int)response.StatusCode}.");
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TaxTransportException("Tax V1 request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
        {
            throw InvalidResponse();
        }
    }

    private static JsonElement GetResultData(JsonElement body, string type)
    {
        var result = Property(body, "result");
        if (Text(result, "packetType") != type) throw InvalidResponse();
        return Property(result, "data");
    }

    private static JsonElement? Match(JsonElement array, Func<JsonElement, bool> predicate)
    {
        if (array.ValueKind != JsonValueKind.Array) throw InvalidResponse();
        var matches = array.EnumerateArray().Where(predicate).Take(2).ToArray();
        return matches.Length switch { 0 => null, 1 => matches[0], _ => throw InvalidResponse() };
    }

    private static JsonElement Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var result) ? result : default;

    private static int? Number(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var number) ? number : null;

    private static string Text(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static Guid? Id(JsonElement element, string name) =>
        Guid.TryParseExact(Text(element, name), "D", out var id) && id != Guid.Empty ? id : null;

    private static TaxTransportException InvalidResponse() => new("Tax V1 authority returned an unreadable or unmatched response.");

    private sealed record Packet(Guid? Uid, string PacketType, object Data, string EncryptionKeyId = "",
        string SymmetricKey = "", string Iv = "", string FiscalId = "", string DataSignature = "")
    {
        public bool Retry => false;
    }
}
