using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Composition;

internal static class TaxationAvailability
{
    internal static bool IsLocalEnabled(IWebHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("Taxation:EnableLocalPreview");
}
