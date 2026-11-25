using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxMemorySettingsTests
{
    private const string FiscalId = "ABC123";
    private const string Seller = "12345678901234";

    [Theory]
    [InlineData(TaxAuthorityProtocol.V1)]
    [InlineData(TaxAuthorityProtocol.V2)]
    public async Task ConnectionCheckReportsAllStagesWithoutSendingAnInvoice(TaxAuthorityProtocol protocol)
    {
        using var key = RSA.Create(2048);
        var certificate = new CertificateRequest("CN=Tax test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = certificate.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var paths = new List<string>();
        HttpResponseMessage Reply(HttpRequestMessage request)
        {
            var path = request.RequestUri!.AbsolutePath;
            paths.Add(path);
            Assert.DoesNotContain("invoice", path);
            if (request.Method == HttpMethod.Head) return new(HttpStatusCode.MethodNotAllowed);
            if (path.EndsWith("GET_TOKEN")) return Json(new { result = new { packetType = "TOKEN_RESULT", data = new { token = "mock-token" } } });
            if (path.EndsWith("GET_FISCAL_INFORMATION"))
                return Json(new { result = new { packetType = "FISCAL_INFORMATION", data = new { economicCode = Seller, fiscalStatus = "ACTIVE" } } });
            if (path.EndsWith("GET_SERVER_INFORMATION"))
                return Json(new { result = new { packetType = "SERVER_INFORMATION", data = new { publicKeys = new[] { new { algorithm = "RSA", purpose = 1, id = "mock-key", key = "mock-public-key" } } } } });
            if (path.EndsWith("nonce"))
                return Json(new { nonce = "a-mock-challenge", expDate = DateTimeOffset.UtcNow.AddMinutes(1).ToString("O") });
            Assert.EndsWith("server-information", path);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            return Json(new { publicKeys = new[] { new { algorithm = "RSA", id = "mock-key", key = "mock-public-key" } } });
        }

        using var clients = new TaxSandboxClients(Client(Reply), Client(Reply), new MockKey(), new MockCert(cert), TimeProvider.System);
        var memory = Memory(protocol);
        memory.CredentialFingerprint = cert.GetCertHashString(HashAlgorithmName.SHA256);
        var results = await new TaxConnectionCheck(clients, new MockCredentials(), new Access(true))
            .CheckAsync(memory, default);
        Assert.Equal(5, results.Count);
        Assert.All(results, step => Assert.Equal("Passed", step.Status));
        Assert.Equal(protocol == TaxAuthorityProtocol.V1 ? 4 : 3, paths.Count);
    }

    [Fact]
    public async Task ProductionAndDisabledSandboxDoNotContactNetwork()
    {
        using var clients = new TaxSandboxClients(Client(_ => throw new InvalidOperationException("Unexpected network")),
            Client(_ => throw new InvalidOperationException("Unexpected network")), null!, null!, TimeProvider.System);
        var check = new TaxConnectionCheck(clients, new MockCredentials(), new Access(false));
        var production = Memory(TaxAuthorityProtocol.V2);
        production.Environment = TaxAuthorityEnvironment.Production;
        Assert.Equal("Skipped", (await check.CheckAsync(production, default))[2].Status);
        Assert.Equal("Failed", (await check.CheckAsync(Memory(TaxAuthorityProtocol.V1), default))[2].Status);
    }

    [Fact]
    public void V1RejectsMismatchedPublicKeyBeforePersistingIt()
    {
        if (!OperatingSystem.IsWindows()) return;
        var directory = Directory.CreateTempSubdirectory("vazin-tax-pair-");
        try
        {
            using var first = RSA.Create(2048);
            using var other = RSA.Create(2048);
            var vault = new TaxConnectionSettings(null!, directory.FullName);
            var error = Assert.Throws<ArgumentException>(() => vault.Import(first.ExportPkcs8PrivateKeyPem(),
                other.ExportSubjectPublicKeyInfoPem()));
            Assert.Contains("مطابقت ندارد", error.Message);
            Assert.Empty(directory.GetFiles());
            var fingerprint = vault.Import(first.ExportPkcs8PrivateKeyPem(), first.ExportSubjectPublicKeyInfoPem());
            using var opened = vault.Open(fingerprint);
            Assert.Equal(first.ExportSubjectPublicKeyInfo(), opened.ExportSubjectPublicKeyInfo());
        }
        finally { directory.Delete(recursive: true); }
    }

    private static TaxMemory Memory(TaxAuthorityProtocol protocol) => new()
    {
        Id = Guid.NewGuid(), FiscalId = FiscalId, SellerEconomicCode = Seller, CredentialFingerprint = new string('A', 64),
        Protocol = protocol, SandboxEnabled = true
    };

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> reply) =>
        new(new MockHandler(reply)) { BaseAddress = new Uri("https://example.invalid/req/") };

    private sealed class MockHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(reply(request));
    }
    private sealed class Access(bool enabled) : ITaxSandboxAccess
    {
        public Task<bool> HasTransportAsync(CancellationToken ct) => Task.FromResult(enabled);
        public Task<bool> IsAllowedAsync(Guid? id, TaxAuthorityProtocol protocol, CancellationToken ct) => Task.FromResult(enabled);
    }
    private sealed class MockKey : ITaxV1SigningKeys
    {
        public RSA Open(string fingerprint) => RSA.Create(2048);
    }
    private sealed class MockCert(X509Certificate2 cert) : ITaxCertificateStore
    {
        public X509Certificate2 Open(string sha256) => new(cert);
    }
    private sealed class MockCredentials : ITaxMemoryCredentials
    {
        public string Prepare(TaxMemorySettingsInput input, TaxMemory current) => throw new NotSupportedException();
        public IReadOnlyList<TaxCertificateOption> ListCertificates() => [];
        public TaxMemoryCredentialStatus Describe(TaxMemory memory) => new(true, "کلید آزمون آماده است.");
    }
}
