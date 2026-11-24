using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxProtocolTests
{
    internal static readonly DateTimeOffset Issued = DateTimeOffset.FromUnixTimeMilliseconds(1683997837988)
        .ToOffset(TimeSpan.FromMinutes(210));

    internal static InvoicePreview Preview(Guid memory) => new(Guid.NewGuid(), memory, "A11216", Issued.Date,
        "Buyer", "10100302746", "Rial", 20000, 500, 1755, 21255,
        [new PreviewLine(1, Guid.NewGuid(), "Item", 2, "2710000138624", "164", 10000, 500, 1755, 21255)], [])
    {
        BuyerNationalId = "10100302746",
        VatPercent = 9
    };

    [Fact]
    public void PayloadAndTaxIdMatchOfficialExample()
    {
        var binding = new TaxSigningBinding(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");
        var preview = Preview(Guid.NewGuid());
        var filing = new TaxFilingDetails(49321217, Issued, 2, 2);
        var (id, raw) = TaxInvoicePayload.Build(preview, binding, filing);
        Assert.Equal("A1121604C220002F095011", id);
        using var json = JsonDocument.Parse(raw);
        var header = json.RootElement.GetProperty("header");
        Assert.Equal("0002F09501", header.GetProperty("inno").GetString());
        Assert.Equal(1683997837988, header.GetProperty("indatim").GetInt64());
        Assert.Equal(21255, header.GetProperty("tbill").GetInt32());
        Assert.Equal(1755, json.RootElement.GetProperty("body")[0].GetProperty("vam").GetInt32());
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview with { VatPercent = 10 }, binding, filing));
        var fractional = preview with { Lines = [preview.Lines[0] with { UnitPrice = 10000.1m }] };
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(fractional, binding, filing));
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview, binding, filing with { Settlement = 3 }));
    }

    [Fact]
    public async Task SignsEachNonceEncryptsInvoiceAndUsesSeparateInquiryResponses()
    {
        using var taxpayerKey = RSA.Create(2048);
        using var authorityKey = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        using var certificate = Certificate(taxpayerKey, now);
        using var authorityCertificate = Certificate(authorityKey, now);
        var nonce = 0;
        var attempt = Guid.NewGuid();
        var reference = Guid.NewGuid();
        const string taxId = "A1121604C220002F095011";
        var paths = new List<string>();
        using var handler = new Stub(async (request, ct) =>
        {
            Assert.Equal("mock.invalid", request.RequestUri!.Host);
            var path = request.RequestUri.AbsolutePath.Split('/')[^1];
            paths.Add(path);
            if (path == "nonce")
            {
                Assert.Null(request.Headers.Authorization);
                return Response(new { nonce = $"nonce-{++nonce}", expDate = now.AddMinutes(1) });
            }

            var token = request.Headers.Authorization!;
            Assert.Equal("Bearer", token.Scheme);
            using var authentication = TaxJose.Verify(token.Parameter!, taxpayerKey);
            Assert.NotNull(authentication);
            Assert.Equal($"nonce-{nonce}", authentication.RootElement.GetProperty("nonce").GetString());
            Assert.Equal("A11216", authentication.RootElement.GetProperty("clientId").GetString());
            if (path == "server-information")
                return Response(new { publicKeys = new[] { new { id = "server", algorithm = "RSA", purpose = 1,
                    key = Convert.ToBase64String(authorityKey.ExportSubjectPublicKeyInfo()) } } });
            if (path == "invoice")
            {
                using var posted = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                var packet = posted.RootElement[0];
                Assert.Equal(attempt, packet.GetProperty("header").GetProperty("requestTraceId").GetGuid());
                var signed = Decrypt(packet.GetProperty("payload").GetString()!, authorityKey);
                using var invoice = TaxJose.Verify(signed, taxpayerKey);
                Assert.NotNull(invoice);
                Assert.Equal(taxId, invoice.RootElement.GetProperty("taxid").GetString());
                return Response(new { result = new[] { new { uid = attempt, referenceNumber = reference } } });
            }
            if (path == "inquiry-by-uid")
            {
                Assert.Contains($"uidList={attempt:D}", request.RequestUri.Query);
                return Response(new[] { new { uid = attempt, fiscalId = "A11216", status = "FAILED",
                    referenceNumber = reference, data = new { error = new[] { "test" } } } });
            }
            if (path == "inquiry-by-reference-id")
            {
                var sign = TaxJose.Sign(JsonSerializer.Serialize(new { referenceNumber = reference, clientId = "A11216", taxId }),
                    authorityCertificate, now);
                return Response(new[] { new { uid = attempt, fiscalId = "A11216", status = "SUCCESS", referenceNumber = reference, sign } });
            }
            Assert.Equal("inquiry-invoice-status", path);
            return Response(new[] { new { taxId, invoiceStatus = "AWAITING_REACTION", article6Status = "NOT_EXCEEDED" } });
        });
        using var http = new HttpClient(handler) { BaseAddress = new("https://mock.invalid/requestsmanager/api/v2/") };
        var client = new MoadianClient(http, new TestCertificates(certificate), TimeProvider.System);
        var submission = new TaxSubmission(Guid.NewGuid(), "A11216", TaxAuthorityEnvironment.Sandbox, "test", taxId,
            """{"taxid":"A1121604C220002F095011"}""", attempt, now, null, TaxDispatchStatus.Pending);
        using var prepared = await client.PrepareAsync(submission, attempt, CancellationToken.None);
        var receipt = await client.SendAsync(prepared, CancellationToken.None);
        Assert.Equal(reference, receipt.ReferenceNumber);
        Assert.Equal("FAILED", (await client.InquireAsync(submission, CancellationToken.None)).Status);
        var withReference = submission with { ReferenceNumber = reference };
        Assert.Equal("SUCCESS", (await client.InquireAsync(withReference, CancellationToken.None)).Status);
        Assert.Equal("AWAITING_REACTION", (await client.GetWorkFolderAsync(withReference, CancellationToken.None)).InvoiceStatus);
        Assert.Equal(paths.Count / 2, nonce);
    }

    private static X509Certificate2 Certificate(RSA key, DateTimeOffset now) =>
        new CertificateRequest("CN=Mock only", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(now.AddDays(-1), now.AddDays(1));

    private static HttpResponseMessage Response(object response) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json") };

    private static string Decrypt(string compact, RSA key)
    {
        var parts = compact.Split('.');
        using var header = JsonDocument.Parse(Decode(parts[0]));
        Assert.Equal("RSA-OAEP-256", header.RootElement.GetProperty("alg").GetString());
        Assert.Equal("A256GCM", header.RootElement.GetProperty("enc").GetString());
        Assert.Equal("server", header.RootElement.GetProperty("kid").GetString());
        var ciphertext = Decode(parts[3]);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key.Decrypt(Decode(parts[1]), RSAEncryptionPadding.OaepSHA256), 16);
        aes.Decrypt(Decode(parts[2]), ciphertext, Decode(parts[4]), plaintext, Encoding.ASCII.GetBytes(parts[0]));
        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] Decode(string text) =>
        Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/').PadRight((text.Length + 3) / 4 * 4, '='));

    private sealed class TestCertificates(X509Certificate2 certificate) : ITaxCertificateStore
    {
        public X509Certificate2 Open(string sha256) => new(certificate);
    }

    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            callback(request, cancellationToken);
    }
}
