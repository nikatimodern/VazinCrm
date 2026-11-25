using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxConnection;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal interface ITaxV1SigningKeys
{
    RSA Open(string fingerprint);
}

internal sealed class TaxConnectionSettings : ITaxConnectionSettings, ITaxV1SigningKeys
{
    private readonly ITaxCertificateStore certificates;
    private readonly string folder;
    private readonly SemaphoreSlim modeChange = new(1, 1);
    private string ProfileFile => Path.Combine(folder, "connection.json");

    public TaxConnectionSettings(ITaxCertificateStore certificates) : this(certificates,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VazinCrm", "MoadianSandbox"))
    {
    }

    internal TaxConnectionSettings(ITaxCertificateStore certificates, string folder)
    {
        this.certificates = certificates;
        this.folder = folder;
    }

    internal sealed record Profile(Guid MemoryId, TaxAuthorityProtocol Protocol, string EconomicCode, string CredentialFingerprint);

    public TaxAuthorityProtocol ActiveProtocol => Read()?.Protocol ?? TaxAuthorityProtocol.V2;

    internal Profile Read()
    {
        return File.Exists(ProfileFile) ? JsonSerializer.Deserialize<Profile>(File.ReadAllBytes(ProfileFile)) : null;
    }

    public async Task SaveAsync(SaveTaxConnectionCommand request, Func<TaxAuthorityProtocol, CancellationToken, Task<bool>> hasOpenSubmissions,
        CancellationToken ct)
    {
        await modeChange.WaitAsync(ct);
        try
        {
            var current = ActiveProtocol;
            if (current != request.Protocol && await hasOpenSubmissions(current, ct))
                throw new ArgumentException("قبل از تغییر روش ارسال، درخواست‌های مالیاتی در جریان را تعیین تکلیف کنید.");
            Save(request);
        }
        finally { modeChange.Release(); }
    }

    internal async Task<T> WithActiveSendAsync<T>(TaxAuthorityProtocol protocol, Func<Task<T>> send, CancellationToken ct)
    {
        Task<T> started;
        await modeChange.WaitAsync(ct);
        try
        {
            if (ActiveProtocol != protocol)
                throw new TaxTransportException("The selected tax protocol changed before sending.");
            started = send();
        }
        finally { modeChange.Release(); }
        return await started;
    }

    internal void Save(SaveTaxConnectionCommand request)
    {
        var current = Read();
        string fingerprint;
        if (request.Protocol == TaxAuthorityProtocol.V1)
        {
            if (string.IsNullOrWhiteSpace(request.V1PrivateKey) && current is { Protocol: TaxAuthorityProtocol.V1 } &&
                current.MemoryId == request.MemoryId)
                fingerprint = current.CredentialFingerprint;
            else
                fingerprint = Import(request.V1PrivateKey);
            using var signingKey = Open(fingerprint);
        }
        else
        {
            fingerprint = request.CertificateSha256?.Trim().ToUpperInvariant();
            if (!IsFingerprint(fingerprint))
                throw new ArgumentException("اثر انگشت SHA-256 گواهی معتبر نیست.");
            using var certificate = certificates.Open(fingerprint);
            using var key = System.Security.Cryptography.X509Certificates.RSACertificateExtensions.GetRSAPrivateKey(certificate);
            if (key is not { KeySize: >= 2048 })
                throw new CryptographicException("Tax signing key is unavailable.");
        }

        Directory.CreateDirectory(folder);
        WriteAtomically(ProfileFile, JsonSerializer.SerializeToUtf8Bytes(
            new Profile(request.MemoryId, request.Protocol, request.SellerEconomicCode, fingerprint)));
    }

    public RSA Open(string fingerprint)
    {
        if (!OperatingSystem.IsWindows() || !IsFingerprint(fingerprint))
            throw new CryptographicException("Pinned V1 signing key is unavailable.");
        byte[] raw;
        try
        {
            raw = ProtectedData.Unprotect(File.ReadAllBytes(KeyFile(fingerprint)), null, DataProtectionScope.CurrentUser);
        }
        catch (IOException)
        {
            throw new CryptographicException("Pinned V1 signing key is unavailable.");
        }

        var key = RSA.Create();
        try
        {
            key.ImportPkcs8PrivateKey(raw, out var read);
            if (read != raw.Length || Fingerprint(key) != fingerprint)
                throw new CryptographicException("Pinned V1 signing key could not be verified.");
            return key;
        }
        catch
        {
            key.Dispose();
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    private string Import(string pem)
    {
        if (!OperatingSystem.IsWindows())
            throw new ArgumentException("ذخیرهٔ کلید نسخه ۱ به حساب اجرای ویندوز نیاز دارد.");
        if (string.IsNullOrWhiteSpace(pem) || pem.Length > 8192)
            throw new ArgumentException("کلید خصوصی RSA در قالب PEM وارد کنید.");
        using var key = RSA.Create();
        try
        {
            key.ImportFromPem(pem);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            throw new ArgumentException("قالب کلید خصوصی RSA قابل خواندن نیست.");
        }

        if (key.KeySize < 2048)
            throw new ArgumentException("کلید خصوصی RSA باید حداقل ۲۰۴۸ بیتی باشد.");

        var fingerprint = Fingerprint(key);
        var file = KeyFile(fingerprint);
        byte[] raw;
        try { raw = key.ExportPkcs8PrivateKey(); }
        catch (CryptographicException) { throw new ArgumentException("فایل باید حاوی کلید خصوصی RSA باشد."); }
        try
        {
            Directory.CreateDirectory(folder);
            if (!File.Exists(file))
                WriteAtomically(file, ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser));
            return fingerprint;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    private static string Fingerprint(RSA key) => Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()));
    private static bool IsFingerprint(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
    private string KeyFile(string fingerprint) => Path.Combine(folder, fingerprint + ".key");

    private static void WriteAtomically(string path, byte[] bytes)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
