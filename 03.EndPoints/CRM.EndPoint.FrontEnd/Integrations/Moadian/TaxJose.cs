using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal static class TaxJose
{
    internal static string Sign(string json, X509Certificate2 certificate, DateTimeOffset now)
    {
        using var key = certificate.GetRSAPrivateKey() ?? throw new CryptographicException("RSA signing key unavailable.");
        var header = JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "RS256", x5c = new[] { Convert.ToBase64String(certificate.RawData) },
            sigT = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            crit = new[] { "sigT" }
        });
        var input = Encode(header) + "." + Encode(Encoding.UTF8.GetBytes(json));
        return input + "." + Encode(key.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    internal static string Encrypt(string signed, RSA key, string keyId)
    {
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RSA-OAEP-256", enc = "A256GCM", kid = keyId }));
        var contentKey = RandomNumberGenerator.GetBytes(32);
        try
        {
            var iv = RandomNumberGenerator.GetBytes(12);
            var plaintext = Encoding.ASCII.GetBytes(signed);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var gcm = new AesGcm(contentKey, tag.Length);
            gcm.Encrypt(iv, plaintext, ciphertext, tag, Encoding.ASCII.GetBytes(header));
            return string.Join(".", header, Encode(key.Encrypt(contentKey, RSAEncryptionPadding.OaepSHA256)),
                Encode(iv), Encode(ciphertext), Encode(tag));
        }
        finally { CryptographicOperations.ZeroMemory(contentKey); }
    }

    internal static JsonDocument Verify(string compact, RSA key)
    {
        var parts = compact.Split('.');
        if (parts.Length != 3)
            return null;
        try
        {
            using var header = JsonDocument.Parse(Decode(parts[0]));
            if (header.RootElement.GetProperty("alg").GetString() != "RS256" ||
                !key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Decode(parts[2]),
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                return null;
            return JsonDocument.Parse(Decode(parts[1]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
        {
            return null;
        }
    }

    private static byte[] Decode(string encoded)
    {
        var text = encoded.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(text.PadRight((text.Length + 3) / 4 * 4, '='));
    }

    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
