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

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxConnectionCheck(TaxSandboxClients clients, ITaxMemoryCredentials credentials,
    ITaxSandboxAccess access) : ITaxConnectionCheck
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
        if (!sandbox)
        {
            steps.SkipOnline("اتصال آنلاین محیط عملیاتی غیرفعال است.");
            return steps.Results;
        }
        if (!configured || !credential.Ready)
            return steps.Results;

        if (!await access.IsAllowedAsync(memory.Id, memory.Protocol, ct))
        {
            steps.SkipOnline("این بررسی به اجرای ERP در محیط توسعه و اجازه اتصال آزمایشی این حافظه نیاز دارد.");
            return steps.Results;
        }

        var stage = 2;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "");
            using var response = await clients.For(memory.Protocol).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode >= HttpStatusCode.MultipleChoices && response.StatusCode < HttpStatusCode.BadRequest)
                throw new TaxTransportException("Unexpected redirect.");
            steps.Record(stage++, true, "پاسخ HTTPS از نشانی مجاز آزمایشی دریافت شد.");

            using var information = memory.Protocol == TaxAuthorityProtocol.V1
                ? await CheckV1Async(memory, steps, ct)
                : await clients.V2.CheckServerInformationAsync(memory, () =>
                    steps.Record(3, true, "چالش امضا شد و درخواست احراز هویت‌شده پذیرفته شد."), ct);

            stage = 4;
            var hasInformation = memory.Protocol == TaxAuthorityProtocol.V1
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
            steps.Record(steps.Results[3].Status == "Passed" ? 4 : stage, false, "این مرحله با پاسخ معتبر تکمیل نشد.");
        }
        return steps.Results;
    }

    private async Task<JsonDocument> CheckV1Async(TaxMemory memory, ConnectionSteps steps, CancellationToken ct)
    {
        await clients.V1.VerifyIdentityAsync(memory.FiscalId,
            new(memory.Environment, null, memory.SellerEconomicCode, TaxAuthorityProtocol.V1, memory.CredentialFingerprint), ct);
        steps.Record(3, true, "توکن دریافت شد و هویت حافظه با اطلاعات سازمان تطبیق داشت.");
        return await clients.V1.ReadServerInformationAsync(ct);
    }

    private sealed class ConnectionSteps
    {
        private static readonly string[] Names = ["تنظیمات حافظه", "کلید یا گواهی", "ارتباط HTTPS", "احراز هویت", "اطلاعات سرور"];
        private readonly TaxConnectionStep[] steps = Names.Select(name => new TaxConnectionStep(name, "Skipped", "بررسی نشد.")).ToArray();
        internal IReadOnlyList<TaxConnectionStep> Results => steps;
        internal void Record(int index, bool passed, string message) =>
            steps[index] = new(Names[index], passed ? "Passed" : "Failed", message);
        internal void SkipOnline(string message)
        {
            for (var index = 2; index < steps.Length; index++)
                steps[index] = new(Names[index], "Skipped", message);
        }
    }
}
