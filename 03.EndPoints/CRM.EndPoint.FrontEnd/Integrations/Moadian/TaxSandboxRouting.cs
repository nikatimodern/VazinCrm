using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

public interface ITaxSandboxAccess
{
    Task<bool> HasTransportAsync(CancellationToken ct);
    Task<bool> IsAllowedAsync(Guid? id, TaxAuthorityProtocol protocol, CancellationToken ct);
}

internal sealed class TaxSandboxRouting(ITaxMemorySettingsRepository repository, IWebHostEnvironment environment,
    IConfiguration configuration, TaxConnectionSettings legacy) : ITaxSandboxAccess
{
    public async Task<bool> HasTransportAsync(CancellationToken ct) =>
        TaxationAvailability.IsLocalEnabled(environment, configuration) &&
        (TaxationAvailability.HasSandboxTransport(environment, configuration) || await repository.HasEnabledSandboxAsync(ct));

    public async Task<bool> IsAllowedAsync(Guid? id, TaxAuthorityProtocol protocol, CancellationToken ct)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return false;
        var memory = id is { } memoryId ? await repository.FindMemoryAsync(memoryId, ct) : null;
        return memory?.SellerEconomicCode is not null
            ? memory.Environment == TaxAuthorityEnvironment.Sandbox && memory.SandboxEnabled
            : TaxationAvailability.TryGetSandboxAddress(environment, configuration, protocol, out _);
    }

    internal async Task<TaxAuthorityProtocol> GetActiveProtocolAsync(CancellationToken ct)
    {
        var active = await repository.FindActiveMemoryAsync(ct);
        return active?.SellerEconomicCode is not null ? active.Protocol : legacy.ActiveProtocol;
    }

    internal static Uri Address(TaxAuthorityProtocol protocol) =>
        new(protocol == TaxAuthorityProtocol.V1 ? "https://sandboxrc.tax.gov.ir/req/" :
            "https://sandboxrc.tax.gov.ir/requestsmanager/api/v2/");
}
