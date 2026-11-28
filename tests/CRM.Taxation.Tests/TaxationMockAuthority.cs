using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Xunit;

namespace CRM.Taxation.Tests;

internal sealed class TaxationMockAuthority : ITaxSigningBindings, IDisposable
{
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly RSA authorityKey = RSA.Create(2048);
    private readonly X509Certificate2 signingCertificate;
    private readonly X509Certificate2 authorityCertificate;
    private readonly HttpClient client;
    private readonly ConcurrentDictionary<Guid, Receipt> receipts = new();
    private int invoicePosts;
    private int nonceRequests;
    private int paymentPosts;

    internal TaxationMockAuthority()
    {
        signingCertificate = TaxProtocolTests.Certificate(signingKey, DateTimeOffset.UtcNow);
        authorityCertificate = TaxProtocolTests.Certificate(authorityKey, DateTimeOffset.UtcNow);
        client = new HttpClient(new TaxProtocolTests.Stub(HandleAsync))
        {
            BaseAddress = new Uri("https://mock.invalid/requestsmanager/api/v2/")
        };
        Transport = new MoadianClient(client, new TaxProtocolTests.TestCertificates(signingCertificate), TimeProvider.System);
    }

    internal MoadianClient Transport { get; }
    internal int InvoicePosts => invoicePosts;
    internal int PaymentPosts => paymentPosts;

    public TaxSigningBinding Get(Guid memoryId) => new(TaxAuthorityEnvironment.Sandbox,
        signingCertificate.GetCertHashString(HashAlgorithmName.SHA256), "14003778990");

    private async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Assert.Equal("mock.invalid", request.RequestUri.Host);
        var path = request.RequestUri.AbsolutePath.Split('/')[^1];
        if (path == "nonce")
            return TaxProtocolTests.Response(new { nonce = $"mock-{Interlocked.Increment(ref nonceRequests)}",
                expDate = DateTimeOffset.UtcNow.AddMinutes(1) });

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        using var authentication = TaxJose.Verify(request.Headers.Authorization!.Parameter!, signingKey);
        Assert.NotNull(authentication);
        Assert.StartsWith("mock-", authentication.RootElement.GetProperty("nonce").GetString());
        var fiscalId = authentication.RootElement.GetProperty("clientId").GetString();
        return path switch
        {
            "server-information" => TaxProtocolTests.Response(new { publicKeys = new[]
            {
                new { id = "server", algorithm = "RSA", purpose = 1, key = Convert.ToBase64String(authorityKey.ExportSubjectPublicKeyInfo()) }
            } }),
            "invoice" => await ReadInvoiceAsync(request, fiscalId, ct),
            "inquiry-by-reference-id" or "inquiry-by-uid" => Processing(request.RequestUri.Query, fiscalId),
            "inquiry-invoice-status" => TaxProtocolTests.Response(receipts.Values
                .Where(x => request.RequestUri.Query.Contains(x.TaxId, StringComparison.Ordinal))
                .Select(x => new { taxId = x.TaxId, invoiceStatus = "AWAITING_REACTION", article6Status = "NOT_EXCEEDED" })),
            "invoice-payment" => await ReadPaymentAsync(request, ct),
            _ => throw new InvalidOperationException($"Unexpected Mock endpoint {path}")
        };
    }

    private async Task<HttpResponseMessage> ReadInvoiceAsync(HttpRequestMessage request, string fiscalId, CancellationToken ct)
    {
        Assert.Equal(HttpMethod.Post, request.Method);
        using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(ct));
        var packet = body.RootElement[0];
        var uid = packet.GetProperty("header").GetProperty("requestTraceId").GetGuid();
        Assert.Equal(fiscalId, packet.GetProperty("header").GetProperty("fiscalId").GetString());
        var jws = TaxProtocolTests.Decrypt(packet.GetProperty("payload").GetString(), authorityKey);
        using var invoice = TaxJose.Verify(jws, signingKey);
        Assert.NotNull(invoice);
        var taxId = invoice.RootElement.GetProperty("header").GetProperty("taxid").GetString();
        var subject = invoice.RootElement.GetProperty("header").GetProperty("ins").GetInt32();
        var receipt = new Receipt(uid, Guid.NewGuid(), taxId);
        Assert.True(receipts.TryAdd(uid, receipt));
        Interlocked.Increment(ref invoicePosts);
        if (subject == 3)
            throw new HttpRequestException("Mock disconnect after receiving cancellation.");
        return TaxProtocolTests.Response(new { result = new[] { new { uid, referenceNumber = receipt.Reference } } });
    }

    private HttpResponseMessage Processing(string query, string fiscalId)
    {
        var receipt = Assert.Single(receipts.Values.Where(x =>
            query.Contains(x.Reference.ToString("D"), StringComparison.OrdinalIgnoreCase) ||
            query.Contains(x.Uid.ToString("D"), StringComparison.OrdinalIgnoreCase)));
        var sign = TaxJose.Sign(JsonSerializer.Serialize(new { referenceNumber = receipt.Reference,
            clientId = fiscalId, taxId = receipt.TaxId }), authorityCertificate, DateTimeOffset.UtcNow);
        return TaxProtocolTests.Response(new[] { new { uid = receipt.Uid, fiscalId, status = "SUCCESS",
            referenceNumber = receipt.Reference, sign } });
    }

    private async Task<HttpResponseMessage> ReadPaymentAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Assert.Equal(HttpMethod.Post, request.Method);
        using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(ct));
        Assert.Contains(receipts.Values, record => record.TaxId == body.RootElement.GetProperty("taxId").GetString());
        Interlocked.Increment(ref paymentPosts);
        return TaxProtocolTests.Response(new { requestStatus = "SUCCESS", error = Array.Empty<object>() });
    }

    public void Dispose()
    {
        client.Dispose();
        authorityCertificate.Dispose();
        signingCertificate.Dispose();
        authorityKey.Dispose();
        signingKey.Dispose();
    }

    private sealed record Receipt(Guid Uid, Guid Reference, string TaxId);
}
