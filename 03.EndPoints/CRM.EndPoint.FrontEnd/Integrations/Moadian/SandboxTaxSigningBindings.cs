using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class SandboxTaxSigningBindings(IConfiguration config, TaxConnectionSettings settings) : ITaxSigningBindings
{
    public TaxSigningBinding Get(Guid memoryId)
    {
        var profile = settings.Read();
        if (profile?.MemoryId == memoryId)
            return profile.Protocol == TaxAuthorityProtocol.V1
                ? new(TaxAuthorityEnvironment.Sandbox, null, profile.EconomicCode, profile.Protocol, profile.CredentialFingerprint)
                : new(TaxAuthorityEnvironment.Sandbox, profile.CredentialFingerprint, profile.EconomicCode);
        if (profile?.Protocol == TaxAuthorityProtocol.V1)
            throw new ArgumentException("حافظهٔ انتخاب‌شده برای ارسال نسخه ۱ تنظیم نشده است.");

        var section = config.GetSection($"Taxation:Sandbox:Memories:{memoryId:D}");
        var thumbprint = section["CertificateSha256"]?.Trim().ToUpperInvariant();
        if (thumbprint is not { Length: 64 } || !thumbprint.All(char.IsAsciiHexDigit))
            throw new ArgumentException("گواهی آزمایشی برای این حافظه تنظیم نشده است.");
        return new TaxSigningBinding(TaxAuthorityEnvironment.Sandbox, thumbprint, section["SellerEconomicCode"]);
    }
}
