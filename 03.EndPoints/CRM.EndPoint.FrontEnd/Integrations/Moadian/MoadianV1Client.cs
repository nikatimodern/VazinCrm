using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class MoadianV1Client(HttpClient http, ITaxV1SigningKeys keys, TimeProvider clock) : ITaxAuthorityClient
{
    private const string Sync = "api/self-tsp/sync/";
    private const string Enqueue = "api/self-tsp/async/normal-enqueue";
    private static readonly JsonSerializerSettings TokenJsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = { new StringEnumConverter() },
        NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
    };

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
        using var info = await ReadServerInformationAsync(ct);
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
        return new PreparedTaxRequest(attemptId, Build(Enqueue, packet, true, token, privateKey, attemptId), TaxAuthorityProtocol.V1, submission.MemoryId);
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
        using var result = await SyncAsync(operation, new Packet(null, operation, data,
                FiscalId: submission.ReferenceNumber.HasValue ? submission.FiscalId : ""),
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

    internal async Task VerifyIdentityAsync(string fiscalId, TaxSigningBinding binding, CancellationToken ct, Action onTokenReceived = null)
    {
        using var key = keys.Open(binding.V1KeyFingerprint);
        var token = await GetTokenAsync(fiscalId, key, ct);
        onTokenReceived?.Invoke();
        using var response = await SyncAsync("GET_FISCAL_INFORMATION",
            new Packet(null, "GET_FISCAL_INFORMATION", null, FiscalId: fiscalId), token, key, ct);
        var info = GetResultData(response.RootElement, "FISCAL_INFORMATION");
        var status = Text(info, "fiscalStatus");
        var seller = Text(info, "economicCode");
        if (string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(seller))
            throw InvalidResponse();
        if (status != "ACTIVE")
            throw new FiscalIdentityException("سامانه مؤدیان وضعیت حافظه را فعال اعلام نکرد.");
        if (seller != binding.SellerEconomicCode)
            throw new FiscalIdentityException("کد اقتصادی ذخیره‌شده با پاسخ سامانه مؤدیان همخوانی ندارد.");
    }

    internal Task<JsonDocument> ReadServerInformationAsync(CancellationToken ct) =>
        SyncAsync("GET_SERVER_INFORMATION", new Packet(null, "GET_SERVER_INFORMATION", null), null, null, ct);

    internal static bool HasServerInformation(JsonElement response) =>
        Property(GetResultData(response, "SERVER_INFORMATION"), "publicKeys") is { ValueKind: JsonValueKind.Array } keys &&
        keys.EnumerateArray().Any(x => Text(x, "algorithm") == "RSA" && Number(x, "purpose") == 1 &&
            Text(x, "id") is { Length: > 0 } && Text(x, "key") is { Length: > 0 });

    internal async Task<string> GetTokenAsync(string fiscalId, RSA key, CancellationToken ct)
    {
        using var request = BuildTokenRequest(fiscalId, key);
        using var result = await ExchangeAsync(request, ct);
        return Text(GetResultData(result.RootElement, "TOKEN_RESULT"), "token") is { Length: > 0 } token
            ? token : throw InvalidResponse();
    }

    private HttpRequestMessage BuildTokenRequest(string fiscalId, RSA key)
    {
        const string operation = "GET_TOKEN";
        const string jsonMediaType = "application/json";
        // The SDK envelope retains null placeholders. Only the header carries the signature.
        var body = JsonConvert.SerializeObject(new
        {
            Signature = (string)null,
            SignatureKeyId = (string)null,
            Packet = new
            {
                Uid = Guid.NewGuid(),
                PacketType = operation,
                FiscalId = fiscalId,
                Data = new { Username = fiscalId },
                Retry = true,
                EncryptionKeyId = (string)null,
                SymmetricKey = (string)null,
                Iv = (string)null,
                DataSignature = (string)null,
                SignatureKeyId = (string)null
            }
        }, Newtonsoft.Json.Formatting.None, TokenJsonSettings);
        var trace = Guid.NewGuid().ToString("D");
        var timestamp = clock.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var authorization = http.DefaultRequestHeaders.Authorization;
        var signature = TaxV1Cryptography.SignUtf8(body + authorization?.ToString() + trace + timestamp, key);

        var request = new HttpRequestMessage(HttpMethod.Post, Sync + operation)
        {
            Content = new StringContent(body, Encoding.UTF8, jsonMediaType)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(jsonMediaType));
        request.Headers.Authorization = authorization;
        request.Headers.Add("requestTraceId", trace);
        request.Headers.Add("timestamp", timestamp);
        request.Headers.Add("signature", signature);
        return request;
    }

    private async Task<JsonDocument> SyncAsync(string operation, Packet packet, string token, RSA key, CancellationToken ct)
    {
        using var request = Build(Sync + operation, packet, false, token, key);
        return await ExchangeAsync(request, ct);
    }

    private HttpRequestMessage Build(string path, Packet packet, bool batch, string token, RSA key, Guid? traceId = null)
    {
        var trace = (traceId ?? Guid.NewGuid()).ToString("D");
        var time = clock.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var headers = new Dictionary<string, string>
        {
            ["requestTraceId"] = trace,
            ["timestamp"] = time
        };
        if (token is not null) headers["Authorization"] = token;
        var payload = batch ? JsonSerializer.SerializeToElement(new[] { packet }, JsonSerializerOptions.Web)
            : JsonSerializer.SerializeToElement(packet, JsonSerializerOptions.Web);
        var signature = key is null ? null : TaxV1Cryptography.Sign(payload, key, headers);
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = batch
                ? JsonContent.Create(new { packets = payload, signature, signatureKeyId = (string)null })
                : signature is null ? JsonContent.Create(new { packet = payload }) : JsonContent.Create(new { packet = payload, signature })
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
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var success = response.IsSuccessStatusCode;
            JsonDocument body = null;
            try
            {
                body = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            }
            catch (JsonException) when (!success) { }

            var code = body is null ? null : ReadErrorCode(body.RootElement);
            if (success && code is null) return body;
            body?.Dispose();
            throw new TaxTransportException("Tax V1 authority rejected the request.",
                success ? null : (int)response.StatusCode, authorityErrorCode: code);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TaxTransportException("Tax V1 request timed out.", timedOut: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
        {
            throw InvalidResponse();
        }
    }

    private static string ReadErrorCode(JsonElement body) =>
        Property(body, "errors") is { ValueKind: JsonValueKind.Array } errors
            ? errors.EnumerateArray().Select(ErrorCode).FirstOrDefault(code => code is not null)
            : ErrorCode(body);

    private static string ErrorCode(JsonElement error)
    {
        var value = Property(error, "errorCode");
        var code = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
        return code is { Length: 4 or 5 } && code.All(char.IsAsciiDigit) ? code : null;
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

    internal sealed class FiscalIdentityException(string message) : ArgumentException(message);

    private sealed record Packet(Guid? Uid, string PacketType, object Data, string EncryptionKeyId = "",
        string SymmetricKey = "", string Iv = "", string FiscalId = "", string DataSignature = "")
    {
        public bool Retry => false;
    }
}
