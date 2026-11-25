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
    public void LegacyRequestNormalizationMatchesIndependentOpenSslVector()
    {
        using var packet = JsonDocument.Parse("""
            {"uid":null,"packetType":"GET_TOKEN","retry":false,"data":{"username":"ABC123"},
             "encryptionKeyId":"","symmetricKey":"","iv":"","fiscalId":"","dataSignature":""}
            """);
        var headers = new Dictionary<string, string>
        {
            ["timestamp"] = "1655185848687",
            ["requestTraceId"] = "00112233-4455-4677-8899-aabbccddeeff"
        };
        const string expected = "ABC123#########GET_TOKEN#00112233-4455-4677-8899-aabbccddeeff#false###1655185848687##";
        // Generated with OpenSSL dgst -sha256 and a throwaway 2048-bit RSA key.
        const string signature = "Z3wxFetGqqq8dMTbLFYqtSGK3be645yTA1x9nKWMKt3YPFHbYoSsKLXSCnGjEwfAQP3VCvMvBZTcEwB7jzZ+L6LpYXazNJd4x26VVvSrBt5Ra2c49yEEPUkPvm0yeJxhOJ/OdLbGBp+4eipIPSvWEQC1Cpm4vjvADPT9Qe0yJHSZfUCH8O54EAIqYo8iXndSQ3QShxxgNW3Wa+3PRott11yD4iH110f6pux9bJPQWZQEajbFqtyBAo2xThX2j7Hn83e4S4Vlb69NLnVWrw0HojRM2sNaLibquLUdyrVNNO0YhZtTo8QU0P33SkT/AjAqOykOsnXADoqE2Fqh8nEszg==";
        using var signer = RSA.Create();
        signer.ImportFromPem("""
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxXp/J/18WvArZRFkWjR3
            9du8HInFEPhY68kJ0A/DmLUlw8L6ng4ZXiva1hwu0CC+CXxZWPf25J1WnNkT3Iey
            Rtxu/XAXTd5sID7UOTyYjWF8DLenS8SpikJDft/4/DxskpbnqqfyaYn2VmZa4ehr
            tbH37WbK6EdnGoiSifb+RUfoGjDy2X7yKSTRY0nPINudviASpk2/j8tQFSJa0AWz
            JElZZWU10Uf7iOBuhqFBgFSynMNB9YIW2RAYZEy3npYqKpTCF+5EaA3Q7CcO7n1+
            BLDJlLQPf6Q0fL+vzFMJNFVAUHe3a4dVN/Ecr7iUaMBX9nR8plmH1AZwBV9wPVXX
            hQIDAQAB
            -----END PUBLIC KEY-----
            """);
        var normalized = TaxV1Cryptography.Normalize(packet.RootElement, headers);
        Assert.Equal(expected, normalized);
        VerifySignature(signer, normalized, signature);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer offline-token")]
    public async Task GetTokenMatchesSepidarSdkWireFormatAndSignsExactUtf8Body(string authorization)
    {
        const string timestamp = "1655185848687";
        using var signer = RSA.Create(2048);
        using var verifier = RSA.Create();
        verifier.ImportParameters(signer.ExportParameters(false));
        var packetIds = new HashSet<Guid>();
        var traceIds = new HashSet<Guid>();
        using var http = Http(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://example.invalid/req/api/self-tsp/sync/GET_TOKEN", request.RequestUri!.AbsoluteUri);
            Assert.Equal("application/json", Assert.Single(request.Headers.Accept).MediaType);
            Assert.Equal("application/json; charset=utf-8", request.Content!.Headers.ContentType!.ToString());
            Assert.Equal(authorization, request.Headers.Authorization?.ToString());
            Assert.Equal(timestamp, Timestamp(request));
            var trace = Trace(request);
            Assert.True(Guid.TryParseExact(trace, "D", out var traceId) && traceId != Guid.Empty);
            Assert.True(traceIds.Add(traceId));

            var body = await request.Content.ReadAsByteArrayAsync();
            using var json = JsonDocument.Parse(body);
            var uid = json.RootElement.GetProperty("packet").GetProperty("uid").GetString();
            Assert.True(Guid.TryParseExact(uid, "D", out var packetId) && packetId != Guid.Empty);
            Assert.NotEqual(traceId, packetId);
            Assert.True(packetIds.Add(packetId));
            // Captured from the Sepidar SDK serializer offline with a fixed UID.
            var expectedBody = $$$"""{"signature":null,"signatureKeyId":null,"packet":{"uid":"{{{uid}}}","packetType":"GET_TOKEN","fiscalId":"ABC123","data":{"username":"ABC123"},"retry":true,"encryptionKeyId":null,"symmetricKey":null,"iv":null,"dataSignature":null,"signatureKeyId":null}}""";
            Assert.Equal(Encoding.UTF8.GetBytes(expectedBody), body);
            Assert.False(request.Headers.Contains("signatureKeyId"));
            VerifySignature(verifier, expectedBody + authorization + trace + timestamp,
                Assert.Single(request.Headers.GetValues("signature")));
            return Token();
        });
        if (authorization is not null)
            http.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authorization);
        var client = new MoadianV1Client(http, null!, new FixedClock(DateTimeOffset.FromUnixTimeMilliseconds(1655185848687)));
        Assert.Equal("mock-token", await client.GetTokenAsync(FiscalId, signer, default));
        Assert.Equal("mock-token", await client.GetTokenAsync(FiscalId, signer, default));
        Assert.Equal(2, packetIds.Count);
    }

    [Fact]
    public void NormalizationUsesFullPathsAndKeepsTwoDigitArrayIndexesInOrder()
    {
        using var payload = JsonDocument.Parse("""
            {"data":{"zz":"last","z":"first"},"data.zoo":"middle","packets":[0,1,2,3,4,5,6,7,8,9,10,11]}
            """);
        Assert.Equal("first#middle#last#0#1#2#3#4#5#6#7#8#9#10#11", TaxV1Cryptography.Normalize(payload.RootElement));
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
                Assert.True(request.Headers.Contains("signature"));
                return Token();
            }
            if (path.EndsWith("/GET_SERVER_INFORMATION"))
            {
                Assert.Null(request.Headers.Authorization);
                Assert.False(body.TryGetProperty("signature", out _));
                return Reply(new { result = new { packetType = "SERVER_INFORMATION", data = new { publicKeys = new[] {
                    new { id = "server-key", algorithm = "RSA", purpose = 1, key = Convert.ToBase64String(authority.ExportSubjectPublicKeyInfo()) } } } } });
            }
            if (path.EndsWith("/normal-enqueue"))
            {
                Assert.Equal("mock-token", request.Headers.Authorization?.Parameter);
                Assert.Equal(attempt.ToString("D"), request.Headers.GetValues("requestTraceId").Single());
                var packet = body.GetProperty("packets")[0];
                Assert.Equal(JsonValueKind.Null, body.GetProperty("signatureKeyId").ValueKind);
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
                var packetSignature = packet.GetProperty("dataSignature").GetString()!;
                VerifySignature(signer, $"1.25#کالا ## الف#1#1#{TaxId}", packetSignature);
                var envelope = string.Join('#', "mock-token", packet.GetProperty("data").GetString()!, packetSignature,
                    "server-key", FiscalId, packet.GetProperty("iv").GetString()!, "INVOICE.V01", "false",
                    packet.GetProperty("symmetricKey").GetString()!, attempt.ToString("D"), Trace(request), Timestamp(request));
                VerifySignature(signer, envelope, body.GetProperty("signature").GetString()!);
                return Reply(new { result = new[] { new { uid = attempt, referenceNumber = receipt } } });
            }
            Assert.EndsWith("/INQUIRY_BY_REFERENCE_NUMBER", path);
            Assert.Equal(receipt, body.GetProperty("packet").GetProperty("data").GetProperty("referenceNumber")[0].GetGuid());
            VerifySyncEnvelope(request, body, signer, receipt.ToString("D"), FiscalId);
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
            VerifySyncEnvelope(request, body.RootElement, signer, $"{FiscalId}#{attempt:D}", "");
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
        using var http = Http(async request =>
        {
            calls++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            if (request.RequestUri!.AbsolutePath.EndsWith("/GET_TOKEN"))
            {
                Assert.True(request.Headers.Contains("signature"));
                return Token();
            }
            Assert.EndsWith("/GET_FISCAL_INFORMATION", request.RequestUri.AbsolutePath);
            VerifySyncEnvelope(request, body.RootElement, signer, "#", FiscalId);
            return Reply(new { result = new { packetType = "FISCAL_INFORMATION",
                data = new { fiscalStatus = "ACTIVE", economicCode = Seller } } });
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

    private static void VerifySyncEnvelope(HttpRequestMessage request, JsonElement body, RSA signer, string data, string fiscalId)
    {
        var packet = body.GetProperty("packet");
        Assert.Equal(fiscalId, packet.GetProperty("fiscalId").GetString());
        Assert.Equal(JsonValueKind.Null, packet.GetProperty("uid").ValueKind);
        Assert.False(body.TryGetProperty("signatureKeyId", out _));
        var trace = Trace(request);
        Assert.True(Guid.TryParseExact(trace, "D", out _));
        var timestamp = Timestamp(request);
        Assert.True(long.TryParse(timestamp, out var milliseconds) && milliseconds > 1_000_000_000_000);
        var operation = packet.GetProperty("packetType").GetString();
        var normalized = string.Join('#', data, "#", "#", fiscalId.Length == 0 ? "#" : fiscalId, "#",
            operation, trace, "false", "#", timestamp, "#");
        if (request.Headers.Authorization is { } auth)
        {
            Assert.Equal("Bearer", auth.Scheme);
            normalized = auth.Parameter + "#" + normalized;
        }
        VerifySignature(signer, normalized, body.GetProperty("signature").GetString()!);
    }

    private static void VerifySignature(RSA signer, string normalized, string signature) =>
        Assert.True(signer.VerifyData(Encoding.UTF8.GetBytes(normalized), Convert.FromBase64String(signature),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), "Signature did not match the independent signing input.");

    private static string Trace(HttpRequestMessage request) => request.Headers.GetValues("requestTraceId").Single();
    private static string Timestamp(HttpRequestMessage request) => request.Headers.GetValues("timestamp").Single();

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

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
