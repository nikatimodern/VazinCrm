using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxConnection;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxV1ProtocolTests
{
    private const string FiscalId = "ABC123";
    private const string Seller = "12345678901234";
    private const int Serial = 42;
    private static readonly string TaxId = TaxInvoicePayload.TaxId(FiscalId, Serial,
        new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero));
    private static readonly string Invoice = $$"""{"header":{"taxid":"{{TaxId}}","inty":1,"inp":1},"body":[{"sstt":"کالا # الف","am":1.25}]}""";

    [Fact]
    public void NormalizationSortsPropertiesAndPreservesArrayOrderAndEscapes()
    {
        using var json = JsonDocument.Parse("""{"z":null,"a":[{"b":2,"a":"x#y"},true,""],"b":0.00001}""");
        Assert.Equal("x##y#2#true###0.00001##", TaxV1Cryptography.Normalize(json.RootElement));
    }

    [Fact]
    public async Task SendsSignedEncryptedPacketAndKeepsReceiptSeparateFromProcessing()
    {
        using var signer = RSA.Create(2048);
        using var authority = RSA.Create(2048);
        var attempt = Guid.NewGuid();
        var receipt = Guid.NewGuid();
        var calls = new List<string>();
        using var http = Http(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            calls.Add(path);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var body = json.RootElement;
            if (path.EndsWith("/GET_TOKEN"))
            {
                Assert.Equal(FiscalId, body.GetProperty("packet").GetProperty("data").GetProperty("username").GetString());
                Assert.Null(request.Headers.Authorization);
                VerifyEnvelope(request, body, "packet", signer);
                return Token();
            }
            if (path.EndsWith("/GET_SERVER_INFORMATION"))
                return Reply(new { result = new { packetType = "SERVER_INFORMATION", data = new { publicKeys = new[] {
                    new { id = "server-key", algorithm = "RSA", purpose = 1, key = Convert.ToBase64String(authority.ExportSubjectPublicKeyInfo()) } } } } });
            if (path.EndsWith("/normal-enqueue"))
            {
                Assert.Equal("mock-token", request.Headers.Authorization?.Parameter);
                Assert.Equal(attempt.ToString("D"), request.Headers.GetValues("requestTraceId").Single());
                VerifyEnvelope(request, body, "packets", signer);
                var packet = body.GetProperty("packets")[0];
                Assert.Equal(attempt, packet.GetProperty("uid").GetGuid());
                Assert.Equal("INVOICE.V01", packet.GetProperty("packetType").GetString());
                Assert.Equal(FiscalId, packet.GetProperty("fiscalId").GetString());
                Assert.Equal("server-key", packet.GetProperty("encryptionKeyId").GetString());
                Assert.False(packet.GetProperty("retry").GetBoolean());
                var secret = authority.Decrypt(Convert.FromBase64String(packet.GetProperty("symmetricKey").GetString()!),
                    RSAEncryptionPadding.OaepSHA256);
                Assert.Equal(32, Convert.FromHexString(Encoding.ASCII.GetString(secret)).Length);
                Assert.Equal(16, Convert.FromHexString(packet.GetProperty("iv").GetString()!).Length);
                Assert.Equal(Encoding.UTF8.GetByteCount(Invoice) + 16, Convert.FromBase64String(packet.GetProperty("data").GetString()!).Length);
                using var source = JsonDocument.Parse(Invoice);
                Assert.Equal(TaxV1Cryptography.Sign(source.RootElement, signer), packet.GetProperty("dataSignature").GetString());
                return Reply(new { result = new[] { new { uid = attempt, referenceNumber = receipt } } });
            }
            Assert.EndsWith("/INQUIRY_BY_REFERENCE_NUMBER", path);
            Assert.Equal(receipt, body.GetProperty("packet").GetProperty("data").GetProperty("referenceNumber")[0].GetGuid());
            return Inquiry(attempt, receipt, "SUCCESS", "SUCCESS");
        });
        var client = new MoadianV1Client(http, new Keys(signer), TimeProvider.System);
        using var request = await client.PrepareAsync(Submission(), attempt, default);
        Assert.Equal(TaxAuthorityProtocol.V1, request.Protocol);
        Assert.Equal(receipt, (await client.SendAsync(request, default)).ReferenceNumber);
        var result = await client.InquireAsync(Submission() with { AttemptId = attempt, ReferenceNumber = receipt }, default);
        Assert.Equal("SUCCESS", result.Status);
        Assert.Equal(5, calls.Count);
    }

    [Theory]
    [InlineData("FAILED")]
    [InlineData("NOT_FOUND")]
    [InlineData("IN_PROGRESS")]
    public async Task InquiresByUidWithoutResending(string status)
    {
        using var signer = RSA.Create(2048);
        var attempt = Guid.NewGuid();
        var calls = 0;
        using var http = Http(async request =>
        {
            calls++;
            if (request.RequestUri!.AbsolutePath.EndsWith("/GET_TOKEN")) return Token();
            Assert.EndsWith("/INQUIRY_BY_UID", request.RequestUri.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var data = body.RootElement.GetProperty("packet").GetProperty("data")[0];
            Assert.Equal(attempt, data.GetProperty("uid").GetGuid());
            Assert.Equal(FiscalId, data.GetProperty("fiscalId").GetString());
            return Inquiry(attempt, Guid.NewGuid(), status, null);
        });
        var result = await new MoadianV1Client(http, new Keys(signer), TimeProvider.System)
            .InquireAsync(Submission() with { AttemptId = attempt }, default);
        Assert.Equal(status == "NOT_FOUND" ? null : status, result?.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task IdentityProbeMakesNoInvoiceRequestAndDoesNotReturnToken()
    {
        using var signer = RSA.Create(2048);
        var calls = 0;
        using var http = Http(request =>
        {
            calls++;
            if (request.RequestUri!.AbsolutePath.EndsWith("/GET_TOKEN")) return Task.FromResult(Token());
            Assert.EndsWith("/GET_FISCAL_INFORMATION", request.RequestUri.AbsolutePath);
            return Task.FromResult(Reply(new { result = new { packetType = "FISCAL_INFORMATION",
                data = new { fiscalStatus = "ACTIVE", economicCode = Seller } } }));
        });
        var client = new MoadianV1Client(http, new Keys(signer), TimeProvider.System);
        await client.VerifyIdentityAsync(FiscalId, new(TaxAuthorityEnvironment.Sandbox, null, Seller,
            TaxAuthorityProtocol.V1, "key"), default);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task TimeoutAndUnmatchedReceiptAreTransportFailures()
    {
        var attempt = Guid.NewGuid();
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/self-tsp/async/normal-enqueue");
        using var request = new PreparedTaxRequest(attempt, message, TaxAuthorityProtocol.V1);
        using var malformed = Http(_ => Task.FromResult(Reply(new { result = new[] {
            new { uid = Guid.NewGuid(), referenceNumber = Guid.NewGuid() } } })));
        using var timeout = Http(_ => throw new TaskCanceledException());
        await Assert.ThrowsAsync<TaxTransportException>(() =>
            new MoadianV1Client(malformed, null!, TimeProvider.System).SendAsync(request, default));
        using var second = new PreparedTaxRequest(attempt, new HttpRequestMessage(HttpMethod.Post, "test"), TaxAuthorityProtocol.V1);
        await Assert.ThrowsAsync<TaxTransportException>(() =>
            new MoadianV1Client(timeout, null!, TimeProvider.System).SendAsync(second, default));
    }

    [Fact]
    public void PrivateKeySurvivesSettingsReloadInEncryptedStorage()
    {
        if (!OperatingSystem.IsWindows()) return;
        var temp = Directory.CreateTempSubdirectory("vazin-tax-key-test-");
        try
        {
            using var generated = RSA.Create(2048);
            var pem = generated.ExportPkcs8PrivateKeyPem();
            var settings = new TaxConnectionSettings(null!, temp.FullName);
            settings.Save(new SaveTaxConnectionCommand { MemoryId = Guid.NewGuid(), Protocol = TaxAuthorityProtocol.V1,
                SellerEconomicCode = Seller, V1PrivateKey = pem });
            var reloaded = new TaxConnectionSettings(null!, temp.FullName);
            var profile = reloaded.Read();
            Assert.Equal(TaxAuthorityProtocol.V1, profile.Protocol);
            using var opened = reloaded.Open(profile.CredentialFingerprint);
            Assert.Equal(generated.ExportSubjectPublicKeyInfo(), opened.ExportSubjectPublicKeyInfo());
            Assert.DoesNotContain("PRIVATE KEY", File.ReadAllText(Path.Combine(temp.FullName, "connection.json")));
            Assert.DoesNotContain("PRIVATE KEY", File.ReadAllText(Assert.Single(Directory.GetFiles(temp.FullName, "*.key"))));
        }
        finally { temp.Delete(recursive: true); }
    }

    [Fact]
    public async Task CannotSwitchWithUnsettledRequestsOrStartAnInactiveProtocol()
    {
        var temp = Directory.CreateTempSubdirectory("vazin-tax-mode-test-");
        try
        {
            var settings = new TaxConnectionSettings(null!, temp.FullName);
            var changed = await Assert.ThrowsAsync<ArgumentException>(() => settings.SaveAsync(
                new SaveTaxConnectionCommand { Protocol = TaxAuthorityProtocol.V1 },
                (protocol, _) => Task.FromResult(protocol == TaxAuthorityProtocol.V2), default));
            Assert.Contains("تعیین تکلیف", changed.Message);
            await Assert.ThrowsAsync<TaxTransportException>(() => settings.WithActiveSendAsync(
                TaxAuthorityProtocol.V1, () => Task.FromResult(true), default));
            Assert.Equal(TaxAuthorityProtocol.V2, settings.ActiveProtocol);
        }
        finally { temp.Delete(recursive: true); }
    }

    private static void VerifyEnvelope(HttpRequestMessage request, JsonElement body, string name, RSA signer)
    {
        var fields = new Dictionary<string, object>
        {
            [name] = body.GetProperty(name),
            ["requestTraceId"] = request.Headers.GetValues("requestTraceId").Single(),
            ["timestamp"] = request.Headers.GetValues("timestamp").Single()
        };
        if (request.Headers.Authorization is { } auth) fields["Authorization"] = auth.Parameter!;
        Assert.Equal(TaxV1Cryptography.Sign(JsonSerializer.SerializeToElement(fields), signer), body.GetProperty("signature").GetString());
    }

    private static TaxSubmission Submission() => new(Guid.NewGuid(), FiscalId, TaxAuthorityEnvironment.Sandbox, null,
        TaxId, Invoice, null, null, null, TaxDispatchStatus.Pending, InvoiceSerial: Serial, Protocol: TaxAuthorityProtocol.V1, V1KeyFingerprint: "key");

    private static HttpResponseMessage Token() => Reply(new { result = new { packetType = "TOKEN_RESULT",
        data = new { token = "mock-token" } } });

    private static HttpResponseMessage Inquiry(Guid uid, Guid reference, string status, string taxResult) =>
        Reply(new { result = new { packetType = "INQUIRY_RESULT", data = new[] {
            new { uid, referenceNumber = reference, fiscalId = FiscalId, status, data = new { taxResult } } } } });

    private static HttpResponseMessage Reply(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private static HttpClient Http(Func<HttpRequestMessage, Task<HttpResponseMessage>> reply) =>
        new(new Handler(reply)) { BaseAddress = new Uri("https://example.invalid/req/") };

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => reply(request);
    }

    private sealed class Keys(RSA signer) : ITaxV1SigningKeys
    {
        public RSA Open(string fingerprint)
        {
            Assert.Equal("key", fingerprint);
            var clone = RSA.Create();
            clone.ImportParameters(signer.ExportParameters(true));
            return clone;
        }
    }
}
