using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxConnectionCheck;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxConnectionCheck(TaxProtocolClients sandboxClients, ITaxMemoryCredentials credentials,
    ITaxSandboxAccess access, [FromKeyedServices(TaxProductionConnectionHandler.ClientKey)] TaxProtocolClients productionClients)
    : ITaxConnectionCheck
{
    public async Task<IReadOnlyList<TaxConnectionStep>> CheckAsync(TaxMemory memory, CancellationToken ct)
    {
        var steps = new ConnectionSteps();
        var sandbox = memory.Environment == TaxAuthorityEnvironment.Sandbox;
        var configured = !string.IsNullOrWhiteSpace(memory.Title) &&
            memory.FiscalId is { Length: 6 } fiscalId && fiscalId.All(char.IsAsciiLetterOrDigit) &&
            memory.SellerEconomicCode is { Length: 11 or 14 } seller && seller.All(char.IsAsciiDigit) &&
            Enum.IsDefined(memory.Environment) && Enum.IsDefined(memory.Protocol);
        steps.Record(0, configured, configured ? "قالب تنظیمات ذخیره‌شده معتبر است." : "تنظیمات حافظه را کامل کنید.");

        var credential = credentials.Describe(memory);
        steps.Record(1, credential.Ready, credential.Message);
        if (!configured || !credential.Ready)
            return steps.Results;

        if (sandbox && !await access.IsAllowedAsync(memory.Id, memory.Protocol, ct))
        {
            steps.SkipOnline("این بررسی به اجرای ERP در محیط توسعه و اجازه اتصال آزمایشی این حافظه نیاز دارد.");
            return steps.Results;
        }

        var clients = sandbox ? sandboxClients : productionClients;
        var v1 = memory.Protocol == TaxAuthorityProtocol.V1;
        var tokenReceived = false;
        var stage = 2;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head,
                sandbox ? "" : TaxProductionConnectionHandler.ReachabilityPath(memory.Protocol));
            using var response = await clients.For(memory.Protocol).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode >= HttpStatusCode.MultipleChoices && response.StatusCode < HttpStatusCode.BadRequest)
                throw new TaxTransportException("Unexpected redirect.");
            steps.Record(stage++, true, sandbox ? "پاسخ HTTPS از سامانه آزمایشی دریافت شد." :
                "پاسخ HTTPS از سامانه عملیاتی دریافت شد.");

            using var information = v1
                ? await CheckV1Async(clients.V1, memory, steps, ct, () => tokenReceived = true)
                : await clients.V2.CheckServerInformationAsync(memory, () =>
                    steps.Record(3, true, "چالش امضا شد و درخواست احراز هویت‌شده پذیرفته شد."), ct);

            stage = 4;
            var hasInformation = v1
                ? MoadianV1Client.HasServerInformation(information.RootElement)
                : information.RootElement.ValueKind == JsonValueKind.Object &&
                  information.RootElement.TryGetProperty("publicKeys", out var keys) && keys.ValueKind == JsonValueKind.Array &&
                  keys.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.Object &&
                    x.TryGetProperty("algorithm", out var alg) && alg.ValueKind == JsonValueKind.String && alg.GetString() == "RSA" &&
                    x.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString()) &&
                    x.TryGetProperty("key", out var key) && key.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(key.GetString()));
            steps.Record(stage, hasInformation, hasInformation ? "اطلاعات و کلید عمومی سرور دریافت شد." :
                "پاسخ اطلاعات سرور معتبر نبود.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested &&
            ex is (HttpRequestException or TaxTransportException or CryptographicException or ArgumentException or OperationCanceledException))
        {
            var failedStage = steps.Results[3].Status == "Passed" ? 4 : stage;
            var operation = v1 && failedStage == 3
                ? tokenReceived ? "توکن دریافت شد. استعلام هویت حافظه: " : "دریافت توکن V1: "
                : "";
            var code = ex is TaxTransportException { AuthorityErrorCode: { } authorityCode }
                ? $" کد خطای سازمان {authorityCode}." : "";
            steps.Record(failedStage, false, operation + DescribeFailure(ex) + code);
        }
        return steps.Results;
    }

    private static string DescribeFailure(Exception error) => error switch
    {
        MoadianV1Client.FiscalIdentityException identity => identity.Message,
        TaxTransportException { HttpStatusCode: 401 } => "سامانه احراز هویت درخواست را نپذیرفت (HTTP 401).",
        TaxTransportException { HttpStatusCode: 403 } => "سامانه اجازه دسترسی به این عملیات را نداد (HTTP 403).",
        TaxTransportException { HttpStatusCode: { } status } => $"سامانه پاسخ HTTP {status} برگرداند.",
        TaxTransportException { TimedOut: true } or OperationCanceledException => "زمان انتظار برای پاسخ سامانه تمام شد.",
        HttpRequestException => "ارتباط با سامانه برقرار نشد یا قطع شد.",
        CryptographicException => "دسترسی به کلید امضا یا امضای درخواست ناموفق بود.",
        _ => "پاسخ قابل تأیید از سامانه دریافت نشد."
    };

    private static async Task<JsonDocument> CheckV1Async(MoadianV1Client client, TaxMemory memory, ConnectionSteps steps,
        CancellationToken ct, Action onTokenReceived)
    {
        await client.VerifyIdentityAsync(memory.FiscalId,
            new(memory.Environment, null, memory.SellerEconomicCode, TaxAuthorityProtocol.V1, memory.CredentialFingerprint), ct,
            onTokenReceived, diagnostic => steps.AddDiagnostic(3, diagnostic));
        steps.Record(3, true, "توکن دریافت شد و هویت حافظه با اطلاعات سازمان تطبیق داشت.");
        return await client.ReadServerInformationAsync(ct, diagnostic => steps.AddDiagnostic(4, diagnostic));
    }

    private sealed class ConnectionSteps
    {
        private static readonly string[] Names = ["تنظیمات حافظه", "کلید یا گواهی", "ارتباط HTTPS", "احراز هویت", "اطلاعات سرور"];
        private readonly TaxConnectionStep[] steps = Names.Select(name => new TaxConnectionStep(name, "Skipped", "بررسی نشد.")).ToArray();
        internal IReadOnlyList<TaxConnectionStep> Results => steps;
        internal void Record(int index, bool passed, string message) =>
            steps[index] = steps[index] with { Status = passed ? "Passed" : "Failed", Message = message };
        internal void AddDiagnostic(int index, TaxConnectionDiagnostic diagnostic) =>
            steps[index] = steps[index] with { Diagnostics = [.. steps[index].Diagnostics ?? [], diagnostic] };
        internal void SkipOnline(string message)
        {
            for (var index = 2; index < steps.Length; index++)
                steps[index] = new(Names[index], "Skipped", message);
        }
    }
}
