using System;
using CRM.Core.Domain.Taxation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Composition;

internal static class TaxationAvailability
{
    internal static bool IsLocalEnabled(IWebHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("Taxation:EnableLocalPreview");

    internal static bool TryGetSandboxAddress(IWebHostEnvironment environment, IConfiguration configuration, out Uri address)
        => TryGetSandboxAddress(environment, configuration, TaxAuthorityProtocol.V2, out address);

    internal static bool HasSandboxTransport(IWebHostEnvironment environment, IConfiguration configuration) =>
        TryGetSandboxAddress(environment, configuration, out _) ||
        TryGetSandboxAddress(environment, configuration, TaxAuthorityProtocol.V1, out _);

    internal static bool TryGetSandboxAddress(IWebHostEnvironment environment, IConfiguration configuration,
        TaxAuthorityProtocol protocol, out Uri address)
    {
        address = null;
        var v1 = protocol == TaxAuthorityProtocol.V1;
        return IsLocalEnabled(environment, configuration) && configuration.GetValue<bool>("Taxation:Sandbox:Enabled") &&
            Uri.TryCreate(configuration[v1 ? "Taxation:Sandbox:V1BaseAddress" : "Taxation:Sandbox:BaseAddress"], UriKind.Absolute, out address) &&
            address.Scheme == Uri.UriSchemeHttps && address.IsDefaultPort &&
            address.Host == "sandboxrc.tax.gov.ir" && address.AbsolutePath == (v1 ? "/req/" : "/requestsmanager/api/v2/") &&
            address.UserInfo.Length == 0 && address.Query.Length == 0 && address.Fragment.Length == 0;
    }
}
