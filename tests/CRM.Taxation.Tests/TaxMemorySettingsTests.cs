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
    [InlineData(TaxAuthorityProtocol.V1, TaxAuthorityEnvironment.Sandbox)]
    [InlineData(TaxAuthorityProtocol.V2, TaxAuthorityEnvironment.Sandbox)]
    [InlineData(TaxAuthorityProtocol.V1, TaxAuthorityEnvironment.Production)]
    [InlineData(TaxAuthorityProtocol.V2, TaxAuthorityEnvironment.Production)]
    public async Task ConnectionCheckReportsAllStagesWithoutSendingAnInvoice(TaxAuthorityProtocol protocol, TaxAuthorityEnvironment environment)
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

        var sandbox = environment == TaxAuthorityEnvironment.Sandbox;
        using var clients = new TaxProtocolClients(Client(sandbox ? Reply : Unexpected), Client(sandbox ? Reply : Unexpected),
            new MockKey(), new MockCert(cert), TimeProvider.System);
        using var production = new TaxProtocolClients(ProductionClient(TaxAuthorityProtocol.V1, sandbox ? Unexpected : Reply),
            ProductionClient(TaxAuthorityProtocol.V2, sandbox ? Unexpected : Reply), new MockKey(), new MockCert(cert), TimeProvider.System);
        var memory = Memory(protocol);
        memory.Environment = environment;
        memory.SandboxEnabled = sandbox;
        memory.CredentialFingerprint = cert.GetCertHashString(HashAlgorithmName.SHA256);
        var results = await new TaxConnectionCheck(clients, new MockCredentials(), new Access(sandbox), production)
            .CheckAsync(memory, default);
        Assert.Equal(5, results.Count);
        Assert.All(results, step => Assert.Equal("Passed", step.Status));
        Assert.Equal(protocol == TaxAuthorityProtocol.V1 ? 4 : 3, paths.Count);
    }

    [Theory]
    [InlineData("token-401", false, "HTTP 401")]
    [InlineData("token-invalid", false, "پاسخ قابل تأیید")]
    [InlineData("token-timeout", false, "زمان انتظار")]
    [InlineData("fiscal-403", true, "HTTP 403")]
    [InlineData("fiscal-inactive", true, "وضعیت حافظه را فعال اعلام نکرد")]
    [InlineData("fiscal-mismatch", true, "کد اقتصادی ذخیره‌شده")]
    public async Task V1ConnectionCheckPinpointsFailureWithoutExposingResponse(string scenario, bool tokenReceived, string reason)
    {
        const string confidential = "sensitive-mock-response-value";
        HttpResponseMessage Reply(HttpRequestMessage request)
        {
            if (request.Method == HttpMethod.Head) return new(HttpStatusCode.MethodNotAllowed);
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/GET_TOKEN"))
            {
                return scenario switch
                {
                    "token-401" => new(HttpStatusCode.Unauthorized) { Content = new StringContent(confidential) },
                    "token-invalid" => Json(new { errorDetail = confidential }),
                    "token-timeout" => throw new TaskCanceledException(),
                    _ => Json(new { result = new { packetType = "TOKEN_RESULT", data = new { token = confidential } } })
                };
            }
            Assert.EndsWith("/GET_FISCAL_INFORMATION", path);
            if (scenario == "fiscal-403")
                return new(HttpStatusCode.Forbidden) { Content = new StringContent(confidential) };
            return Json(new
            {
                result = new
                {
                    packetType = "FISCAL_INFORMATION",
                    data = new
                    {
                        fiscalStatus = scenario == "fiscal-inactive" ? "INACTIVE" : "ACTIVE",
                        economicCode = scenario == "fiscal-mismatch" ? "12345678901235" : Seller
                    }
                }
            });
        }

        using var production = new TaxProtocolClients(ProductionClient(TaxAuthorityProtocol.V1, Reply),
            Client(Unexpected), new MockKey(), null!, TimeProvider.System);
        var memory = Memory(TaxAuthorityProtocol.V1);
        memory.Environment = TaxAuthorityEnvironment.Production;
        var results = await new TaxConnectionCheck(null!, new MockCredentials(), new Access(false), production)
            .CheckAsync(memory, default);
        Assert.Equal(["Passed", "Passed", "Passed", "Failed", "Skipped"], results.Select(x => x.Status));
        Assert.Contains(tokenReceived ? "توکن دریافت شد" : "دریافت توکن V1", results[3].Message);
        Assert.Contains(reason, results[3].Message);
        Assert.All(results, step => Assert.DoesNotContain(confidential, step.Message));
    }

    [Fact]
    public async Task InvalidProductionAndDisabledSandboxDoNotContactNetwork()
    {
        using var clients = new TaxProtocolClients(Client(Unexpected), Client(Unexpected), null!, null!, TimeProvider.System);
        var check = new TaxConnectionCheck(clients, new MockCredentials(), new Access(false), clients);
        var production = Memory(TaxAuthorityProtocol.V2);
        production.Environment = TaxAuthorityEnvironment.Production;
        production.SellerEconomicCode = "invalid";
        var productionResults = await check.CheckAsync(production, default);
        Assert.Equal(["Failed", "Passed", "Skipped", "Skipped", "Skipped"], productionResults.Select(x => x.Status));

        var sandboxResults = await check.CheckAsync(Memory(TaxAuthorityProtocol.V1), default);
        Assert.Equal(["Passed", "Passed", "Skipped", "Skipped", "Skipped"], sandboxResults.Select(x => x.Status));
        Assert.Contains("آزمایشی", sandboxResults[2].Message);

    }

    [Theory]
    [InlineData(TaxAuthorityProtocol.V1, "POST", "api/self-tsp/async/normal-enqueue")]
    [InlineData(TaxAuthorityProtocol.V1, "POST", "https://example.invalid/req/api/self-tsp/sync/GET_TOKEN")]
    [InlineData(TaxAuthorityProtocol.V2, "POST", "invoice")]
    [InlineData(TaxAuthorityProtocol.V2, "POST", "payment")]
    [InlineData(TaxAuthorityProtocol.V2, "GET", "https://sandboxrc.tax.gov.ir/requestsmanager/api/v2/server-information")]
    public async Task ProductionCheckBlocksRequestsOutsideReadOnlyContract(TaxAuthorityProtocol protocol, string method, string path)
    {
        using var client = ProductionClient(protocol, Unexpected);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        await Assert.ThrowsAsync<CRM.Core.ApplicationServices.Taxation.Submission.TaxTransportException>(() => client.SendAsync(request));
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
        Id = Guid.NewGuid(), Title = "Tax memory", FiscalId = FiscalId, SellerEconomicCode = Seller, CredentialFingerprint = new string('A', 64),
        Protocol = protocol, SandboxEnabled = true
    };

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> reply) =>
        new(new MockHandler(reply)) { BaseAddress = new Uri("https://example.invalid/req/") };
    private static HttpClient ProductionClient(TaxAuthorityProtocol protocol, Func<HttpRequestMessage, HttpResponseMessage> reply) =>
        new(new TaxProductionConnectionHandler(protocol, new MockHandler(reply)))
        {
            BaseAddress = TaxProductionConnectionHandler.Address(protocol)
        };
    private static HttpResponseMessage Unexpected(HttpRequestMessage _) => throw new InvalidOperationException("Unexpected network");

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
