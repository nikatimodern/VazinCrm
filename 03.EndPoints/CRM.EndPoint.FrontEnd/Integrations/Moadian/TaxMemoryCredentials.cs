using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxMemoryCredentials(TaxConnectionSettings keys, ITaxCertificateStore certificates) : ITaxMemoryCredentials
{
    public string Prepare(TaxMemorySettingsInput input, TaxMemory current)
    {
        var existing = current?.Protocol == input.Protocol ? current.CredentialFingerprint : null;
        if (input.Protocol == TaxAuthorityProtocol.V1)
        {
            if (input.Pfx is not null || !string.IsNullOrWhiteSpace(input.CertificateSha256))
                throw new ArgumentException("گواهی امضا برای نسخه ۱ قابل ثبت نیست.");
            if (!string.IsNullOrWhiteSpace(input.PrivateKeyPem))
            {
                if (string.IsNullOrWhiteSpace(input.PublicKeyPem))
                    throw new ArgumentException("برای ثبت یا جایگزینی کلید خصوصی، فایل کلید عمومی متناظر را هم انتخاب کنید.");
                return keys.Import(input.PrivateKeyPem, input.PublicKeyPem);
            }
            if (existing is null)
                throw new ArgumentException("فایل کلیدهای عمومی و خصوصی نسخه ۱ را انتخاب کنید.");
            if (!string.IsNullOrWhiteSpace(input.PublicKeyPem))
                TaxConnectionSettings.VerifyPublicKey(input.PublicKeyPem, existing);
            using var privateKey = keys.Open(existing);
            return existing;
        }

        if (!string.IsNullOrWhiteSpace(input.PrivateKeyPem) || !string.IsNullOrWhiteSpace(input.PublicKeyPem))
            throw new ArgumentException("فایل کلید خام برای نسخه ۲ قابل ثبت نیست.");
        if (input.Pfx is { Length: > 0 } pfx)
        {
            try { return Install(pfx, input.PfxPassword); }
            finally { CryptographicOperations.ZeroMemory(pfx); }
        }

        var fingerprint = input.CertificateSha256?.Trim().ToUpperInvariant() ?? existing;
        if (!TaxConnectionSettings.IsValidFingerprint(fingerprint))
            throw new ArgumentException("یک گواهی معتبر از فهرست ویندوز انتخاب کنید.");
        using var certificate = certificates.Open(fingerprint);
        if (!CanSign(certificate))
            throw new ArgumentException("حساب اجرای برنامه به کلید خصوصی گواهی دسترسی امضا ندارد.");
        return fingerprint;
    }

    public TaxMemoryCredentialStatus Describe(TaxMemory memory)
    {
        if (string.IsNullOrWhiteSpace(memory.CredentialFingerprint))
            return new(false, "کلید یا گواهی هنوز ثبت نشده است.");
        try
        {
            if (memory.Protocol == TaxAuthorityProtocol.V1)
            {
                using var key = keys.Open(memory.CredentialFingerprint);
                return new(true, "جفت کلید RSA تطبیق داده شده و کلید خصوصی در دسترس است.");
            }

            using var cert = certificates.Open(memory.CredentialFingerprint);
            if (!CanSign(cert))
                return new(false, "کلید خصوصی گواهی در دسترس حساب برنامه نیست.");
            return new(true, $"گواهی معتبر است و کلید خصوصی در دسترس است. انقضا {cert.NotAfter:yyyy-MM-dd}.");
        }
        catch (Exception ex) when (ex is CryptographicException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return new(false, "کلید یا گواهی معتبر در دسترس حساب برنامه نیست.");
        }
    }

    public IReadOnlyList<TaxCertificateOption> ListCertificates()
    {
        if (!OperatingSystem.IsWindows()) return [];
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        try { store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly); }
        catch (Exception ex) when (ex is CryptographicException or UnauthorizedAccessException) { return []; }
        var list = new List<TaxCertificateOption>();
        foreach (var cert in store.Certificates)
        {
            using (cert)
            {
                if (!IsInDate(cert)) continue;
                using var publicKey = cert.GetRSAPublicKey();
                if (publicKey is not { KeySize: >= 2048 }) continue;
                list.Add(new(cert.GetCertHashString(HashAlgorithmName.SHA256), cert.Subject, cert.Issuer,
                    cert.NotAfter.ToUniversalTime(), CanSign(cert)));
            }
        }
        return list.OrderBy(x => x.Subject, StringComparer.Ordinal).ThenBy(x => x.Fingerprint, StringComparer.Ordinal).ToArray();
    }

    private static string Install(byte[] pfx, string password)
    {
        if (!OperatingSystem.IsWindows() || pfx.Length > 64 * 1024 || password is { Length: > 256 })
            throw new ArgumentException("فایل PFX/P12 باید معتبر و حداکثر ۶۴ کیلوبایت باشد.");
        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
            var entries = X509CertificateLoader.LoadPkcs12Collection(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
            string fingerprint;
            try
            {
                var privateCertificates = entries.Where(x => x.HasPrivateKey).ToArray();
                if (privateCertificates.Length != 1 || !IsInDate(privateCertificates[0]) || !CanSign(privateCertificates[0]))
                    throw new ArgumentException("فایل باید فقط یک گواهی RSA معتبر به همراه کلید خصوصی داشته باشد.");
                fingerprint = privateCertificates[0].GetCertHashString(HashAlgorithmName.SHA256);
            }
            finally
            {
                foreach (var certificate in entries) certificate.Dispose();
            }

            using var installed = X509CertificateLoader.LoadPkcs12(pfx, password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            if (installed.GetCertHashString(HashAlgorithmName.SHA256) != fingerprint || !CanSign(installed))
                throw new ArgumentException("کلید خصوصی قابل نصب یا امضا نیست.");
            store.Add(installed);
            return fingerprint;
        }
        catch (Exception ex) when (ex is CryptographicException or UnauthorizedAccessException)
        {
            throw new ArgumentException("رمز یا فایل گواهی معتبر نیست، یا حساب سرور مجوز نصب در مخزن ویندوز ندارد.");
        }
    }

    private static bool IsInDate(X509Certificate2 cert) =>
        DateTime.UtcNow >= cert.NotBefore.ToUniversalTime() && DateTime.UtcNow <= cert.NotAfter.ToUniversalTime();

    private static bool CanSign(X509Certificate2 certificate)
    {
        try
        {
            using var key = certificate.GetRSAPrivateKey();
            return key is { KeySize: >= 2048 } &&
                key.SignHash(new byte[32], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Length > 0;
        }
        catch (Exception ex) when (ex is CryptographicException or UnauthorizedAccessException) { return false; }
    }
}
