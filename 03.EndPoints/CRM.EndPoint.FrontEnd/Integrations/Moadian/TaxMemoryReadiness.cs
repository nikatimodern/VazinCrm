using System;
using CRM.Core.Domain.Taxation;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.EndPoint.FrontEnd.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

public interface ITaxMemoryReadiness
{
    string DescribeSandbox(Guid memoryId);
}

internal sealed class TaxMemoryReadiness(IWebHostEnvironment environment, IConfiguration configuration,
    ITaxSigningBindings bindings, ITaxCertificateStore certificates, ITaxV1SigningKeys keys) : ITaxMemoryReadiness
{
    public string DescribeSandbox(Guid memoryId)
    {
        try
        {
            var binding = bindings.Get(memoryId);
            if (!TaxationAvailability.TryGetSandboxAddress(environment, configuration, binding.Protocol, out _))
                return "Sandbox برای این روش ارسال غیرفعال است.";
            if (binding.Protocol == TaxAuthorityProtocol.V1)
            {
                using var privateKey = keys.Open(binding.V1KeyFingerprint);
                return "کلید امضای نسخه ۱ در دسترس است.";
            }
            if (binding.SellerEconomicCode is not { Length: 11 or 14 } || !binding.SellerEconomicCode.All(char.IsAsciiDigit))
                return "شناسه اقتصادی فروشنده معتبر نیست.";
            using var certificate = certificates.Open(binding.CertificateSha256);
            using var key = certificate.GetRSAPrivateKey();
            return key is { KeySize: >= 2048 } ? "گواهی معتبر و کلید قابل دسترسی است." : "کلید RSA معتبر در دسترس نیست.";
        }
        catch (ArgumentException) { return "اطلاعات اتصال آزمایشی برای حافظه تنظیم نشده است."; }
        catch (CryptographicException) { return "گواهی یا کلید امضا در دسترس نیست."; }
    }
}
