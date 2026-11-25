using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Composition;

internal static class TaxationAvailability
{
    internal static bool IsLocalEnabled(IWebHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("Taxation:EnableLocalPreview");

    internal static bool TryGetSandboxAddress(IWebHostEnvironment environment, IConfiguration configuration, out Uri address)
    {
        address = null;
        return IsLocalEnabled(environment, configuration) && configuration.GetValue<bool>("Taxation:Sandbox:Enabled") &&
            Uri.TryCreate(configuration["Taxation:Sandbox:BaseAddress"], UriKind.Absolute, out address) &&
            address.Scheme == Uri.UriSchemeHttps && address.IsDefaultPort &&
            address.Host == "sandboxrc.tax.gov.ir" && address.AbsolutePath == "/requestsmanager/api/v2/" &&
            address.UserInfo.Length == 0 && address.Query.Length == 0 && address.Fragment.Length == 0;
    }
}
