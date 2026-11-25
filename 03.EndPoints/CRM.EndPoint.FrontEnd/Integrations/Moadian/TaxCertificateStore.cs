using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal interface ITaxCertificateStore
{
    X509Certificate2 Open(string sha256);
}

internal sealed class TaxCertificateStore : ITaxCertificateStore
{
    public X509Certificate2 Open(string sha256)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        X509Certificate2 match = null;
        var now = DateTime.UtcNow;
        foreach (var cert in store.Certificates)
        {
            if (match is null && cert.HasPrivateKey &&
                string.Equals(cert.GetCertHashString(HashAlgorithmName.SHA256), sha256, StringComparison.Ordinal) &&
                now >= cert.NotBefore.ToUniversalTime() && now <= cert.NotAfter.ToUniversalTime())
                match = cert;
            else
                cert.Dispose();
        }
        return match ?? throw new CryptographicException("Pinned tax signing certificate unavailable.");
    }
}
