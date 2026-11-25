using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class MoadianClient(HttpClient http, ITaxCertificateStore certificates, TimeProvider clock) : ITaxAuthorityClient, ITaxPaymentTransport
{
    public async Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct)
    {
        using var cert = OpenCertificate(submission);
        ValidatePayloadIdentity(submission);
        using var server = await GetAsync(submission, cert, "server-information", ct);
        var keys = server.RootElement.GetProperty("publicKeys").EnumerateArray()
            .Where(k => Text(k, "algorithm") == "RSA" && k.GetProperty("purpose").GetInt32() == 1).ToArray();
        if (keys.Length != 1 || string.IsNullOrWhiteSpace(Text(keys[0], "id")))
            throw new TaxTransportException("Tax authority encryption key could not be selected.");
        using var key = RSA.Create();
        var publicKey = Convert.FromBase64String(Text(keys[0], "key"));
        key.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
        if (bytesRead != publicKey.Length || key.KeySize < 2048)
            throw new CryptographicException("Invalid tax authority encryption key.");
        var signed = TaxJose.Sign(submission.Payload, cert, clock.GetUtcNow());
        var encrypted = TaxJose.Encrypt(signed, key, Text(keys[0], "id"));
        var token = await AuthenticateAsync(submission, cert, ct);
        return new PreparedTaxRequest(attemptId, new HttpRequestMessage(HttpMethod.Post, "invoice")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(new[] { new
            {
                header = new { requestTraceId = attemptId, fiscalId = submission.FiscalId },
                payload = encrypted
            } })
        }, memoryId: submission.MemoryId);
    }

    public async Task<PreparedTaxRequest> PreparePaymentAsync(TaxSubmission invoice, Guid attemptId, string payload,
        CancellationToken ct)
    {
        using var content = JsonDocument.Parse(payload);
        if (Text(content.RootElement, "taxId") != invoice.TaxId)
            throw new CryptographicException("Frozen payment does not match the invoice.");
        using var certificate = OpenCertificate(invoice);
        var token = await AuthenticateAsync(invoice, certificate, ct);
        var message = new HttpRequestMessage(HttpMethod.Post, "invoice-payment")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        };
        return new PreparedTaxRequest(attemptId, message, memoryId: invoice.MemoryId);
    }

    public async Task<TaxPaymentReply> SubmitPaymentAsync(PreparedTaxRequest request, CancellationToken ct)
    {
        using var content = await ExchangeAsync(request.Message, ct);
        var response = content.RootElement;
        var state = Text(response, "requestStatus") switch
        {
            "SUCCESS" => TaxPaymentStatus.Accepted,
            "FAILED" => TaxPaymentStatus.Rejected,
            _ => throw InvalidResponse()
        };
        return new TaxPaymentReply(state, response.GetRawText());
    }

    public async Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct)
    {
        using var body = await ExchangeAsync(request.Message, ct);
        if (body.RootElement.ValueKind != JsonValueKind.Object ||
            !body.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            throw InvalidResponse();
        var matched = Match(result, x => Id(x, "uid") == request.AttemptId);
        if (matched is not { } entry || Id(entry, "referenceNumber") is not { } reference)
            throw InvalidResponse();
        return new TaxAcknowledgement(reference, entry.GetRawText());
    }

    public async Task<TaxProcessingResult> InquireAsync(TaxSubmission submission, CancellationToken ct)
    {
        var started = submission.StartedAt ?? throw new InvalidOperationException("Submission has no start time.");
        var start = started.AddMinutes(-5).ToString("O", CultureInfo.InvariantCulture);
        var now = clock.GetUtcNow();
        var query = submission.ReferenceNumber.HasValue
            ? $"inquiry-by-reference-id?referenceIds={submission.ReferenceNumber:D}"
            : $"inquiry-by-uid?uidList={submission.AttemptId:D}&fiscalId={Uri.EscapeDataString(submission.FiscalId)}";
        var upper = started.AddDays(1);
        var end = (now < upper ? now : upper).ToString("O", CultureInfo.InvariantCulture);
        query += $"&start={Uri.EscapeDataString(start)}&end={Uri.EscapeDataString(end)}";
        using var cert = OpenCertificate(submission);
        using var body = await GetAsync(submission, cert, query, ct);
        var matched = Match(body.RootElement, x => Id(x, "uid") == submission.AttemptId &&
            Text(x, "fiscalId") == submission.FiscalId);
        if (matched is not { } entry)
            return null;
        var status = Text(entry, "status");
        if (status == "NOT_FOUND")
            return null;
        var reference = Id(entry, "referenceNumber");
        if (submission.ReferenceNumber.HasValue && reference != submission.ReferenceNumber)
            throw InvalidResponse();
        if (status == "SUCCESS")
            await VerifyReceiptAsync(submission, cert, entry, reference, ct);
        if (status is not ("SUCCESS" or "FAILED" or "IN_PROGRESS" or "TIMEOUT"))
            throw InvalidResponse();
        return new TaxProcessingResult(reference, status, entry.GetRawText());
    }

    public async Task<TaxWorkFolderResult> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct)
    {
        using var cert = OpenCertificate(submission);
        using var body = await GetAsync(submission, cert,
            $"inquiry-invoice-status?taxIds={Uri.EscapeDataString(submission.TaxId)}", ct);
        var matched = Match(body.RootElement, x => Text(x, "taxId") == submission.TaxId);
        return matched is { } entry
            ? new TaxWorkFolderResult(Text(entry, "invoiceStatus"), Text(entry, "article6Status"), Text(entry, "error"), entry.GetRawText())
            : null;
    }

    private X509Certificate2 OpenCertificate(TaxSubmission submission)
    {
        if (!TaxInvoicePayload.MatchesTaxId(submission.FiscalId, submission.TaxId, submission.InvoiceSerial))
            throw new CryptographicException("Pinned tax invoice identity is invalid.");
        return OpenSigningCertificate(submission.CertificateSha256);
    }

    private X509Certificate2 OpenSigningCertificate(string fingerprint)
    {
        var certificate = certificates.Open(fingerprint);
        var now = clock.GetUtcNow();
        if (certificate.HasPrivateKey &&
            string.Equals(certificate.GetCertHashString(HashAlgorithmName.SHA256), fingerprint, StringComparison.Ordinal) &&
            now >= certificate.NotBefore.ToUniversalTime() && now <= certificate.NotAfter.ToUniversalTime())
            return certificate;
        certificate.Dispose();
        throw new CryptographicException("Pinned tax signing certificate is invalid or expired.");
    }

    private static void ValidatePayloadIdentity(TaxSubmission submission)
    {
        try
        {
            using var body = JsonDocument.Parse(submission.Payload);
            if (body.RootElement.ValueKind == JsonValueKind.Object &&
                body.RootElement.TryGetProperty("header", out var header) && header.ValueKind == JsonValueKind.Object &&
                Text(header, "taxid") == submission.TaxId &&
                Text(header, "irtaxid") == submission.Reference?.TaxId &&
                (submission.Reference is null || (header.TryGetProperty("ins", out var subject) &&
                    subject.ValueKind == JsonValueKind.Number && subject.TryGetInt32(out var code) &&
                    code == (int)submission.Kind + 1)) &&
                Text(header, "inno") == submission.InvoiceSerial.Value.ToString("X10", CultureInfo.InvariantCulture) &&
                header.TryGetProperty("indatim", out var issued) && issued.ValueKind == JsonValueKind.Number &&
                issued.TryGetInt64(out var ms) && ms is >= 0 and <= 253402300799999 &&
                TaxInvoicePayload.TaxId(submission.FiscalId, submission.InvoiceSerial.Value,
                    DateTimeOffset.FromUnixTimeMilliseconds(ms)) == submission.TaxId)
                return;
        }
        catch (JsonException)
        {
            throw new CryptographicException("Pinned tax payload is unreadable.");
        }
        throw new CryptographicException("Pinned tax payload does not match the submission.");
    }

    private async Task VerifyReceiptAsync(TaxSubmission submission, X509Certificate2 certificate,
        JsonElement result, Guid? reference, CancellationToken ct)
    {
        if (!reference.HasValue || Text(result, "sign") is not { } signature)
            throw InvalidResponse();
        using var server = await GetAsync(submission, certificate, "server-information", ct);
        foreach (var candidate in server.RootElement.GetProperty("publicKeys").EnumerateArray()
            .Where(x => Text(x, "algorithm") == "RSA"))
        {
            using var key = RSA.Create();
            var bytes = Convert.FromBase64String(Text(candidate, "key"));
            key.ImportSubjectPublicKeyInfo(bytes, out var read);
            if (read != bytes.Length || key.KeySize < 2048)
                continue;
            using var receipt = TaxJose.Verify(signature, key);
            if (receipt is not null && Id(receipt.RootElement, "referenceNumber") == reference &&
                Text(receipt.RootElement, "clientId") == submission.FiscalId &&
                Text(receipt.RootElement, "taxId") == submission.TaxId)
                return;
        }
        throw InvalidResponse();
    }

    internal async Task<JsonDocument> CheckServerInformationAsync(TaxMemory memory, Action authenticated, CancellationToken ct)
    {
        var credentials = new TaxSubmission(Guid.Empty, memory.FiscalId, memory.Environment, memory.CredentialFingerprint,
            null, null, null, null, null, TaxDispatchStatus.Pending);
        using var certificate = OpenSigningCertificate(memory.CredentialFingerprint);
        return await GetAsync(credentials, certificate, "server-information", ct, authenticated);
    }

    private async Task<JsonDocument> GetAsync(TaxSubmission submission, X509Certificate2 cert, string path, CancellationToken ct, Action onAccepted = null)
    {
        var token = await AuthenticateAsync(submission, cert, ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await ExchangeAsync(request, ct, onAccepted);
    }

    private async Task<string> AuthenticateAsync(TaxSubmission submission, X509Certificate2 cert, CancellationToken ct)
    {
        if (submission.Environment != TaxAuthorityEnvironment.Sandbox)
            throw new InvalidOperationException("Only Sandbox credentials are supported for manual dispatch.");
        using var request = new HttpRequestMessage(HttpMethod.Get, "nonce?timeToLive=30");
        using var body = await ExchangeAsync(request, ct);
        var nonce = Text(body.RootElement, "nonce");
        if (string.IsNullOrWhiteSpace(nonce) ||
            !DateTimeOffset.TryParse(Text(body.RootElement, "expDate"), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expires) || expires <= clock.GetUtcNow())
            throw InvalidResponse();
        var now = clock.GetUtcNow();
        if (now > cert.NotAfter.ToUniversalTime())
            throw new CryptographicException("Pinned tax signing certificate expired before authentication.");
        return TaxJose.Sign(JsonSerializer.Serialize(new { nonce, clientId = submission.FiscalId }), cert, now);
    }

    private async Task<JsonDocument> ExchangeAsync(HttpRequestMessage request, CancellationToken ct, Action onAccepted = null)
    {
        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new TaxTransportException($"Tax authority returned HTTP {(int)response.StatusCode}.");
            onAccepted?.Invoke();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TaxTransportException("Tax authority request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
        {
            throw InvalidResponse();
        }
    }

    private static JsonElement? Match(JsonElement array, Func<JsonElement, bool> predicate)
    {
        if (array.ValueKind != JsonValueKind.Array)
            throw InvalidResponse();
        var matches = array.EnumerateArray().Where(predicate).Take(2).ToArray();
        return matches.Length switch { 0 => null, 1 => matches[0], _ => throw InvalidResponse() };
    }

    private static string Text(JsonElement element, string field) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(field, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Guid? Id(JsonElement element, string field) =>
        Guid.TryParse(Text(element, field), out var id) && id != Guid.Empty ? id : null;

    private static TaxTransportException InvalidResponse() => new("Tax authority returned an unreadable or unmatched response.");
}
