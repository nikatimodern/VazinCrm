extern alias TaxCrypto;

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TaxCrypto::Org.BouncyCastle.Crypto.Engines;
using TaxCrypto::Org.BouncyCastle.Crypto.Modes;
using TaxCrypto::Org.BouncyCastle.Crypto.Parameters;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal static class TaxV1Cryptography
{
    internal static string Sign(JsonElement value, RSA key) =>
        Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(Normalize(value)), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));

    internal static string Normalize(JsonElement value) => string.Join('#', Values(value));

    private static System.Collections.Generic.IEnumerable<string> Values(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)
            .SelectMany(x => Values(x.Value)),
        JsonValueKind.Array => value.EnumerateArray().SelectMany(Values),
        _ => [Scalar(value)]
    };

    private static string Scalar(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return "#";
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number.ToString("0.############################", CultureInfo.InvariantCulture),
            _ => value.GetRawText()
        };
        return string.IsNullOrEmpty(text) ? "#" : text.Replace("#", "##", StringComparison.Ordinal);
    }

    internal static (string Data, string SymmetricKey, string Iv) Encrypt(string json, RSA authorityKey)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(16);
        var plaintext = Encoding.UTF8.GetBytes(json);
        var encodedKey = Encoding.ASCII.GetBytes(Convert.ToHexStringLower(key));
        try
        {
            for (var index = 0; index < plaintext.Length; index++)
                plaintext[index] ^= key[index % key.Length];

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), 128, iv));
            var encrypted = new byte[cipher.GetOutputSize(plaintext.Length)];
            var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, encrypted, 0);
            cipher.DoFinal(encrypted, length);
            return (Convert.ToBase64String(encrypted),
                Convert.ToBase64String(authorityKey.Encrypt(encodedKey, RSAEncryptionPadding.OaepSHA256)),
                Convert.ToHexStringLower(iv));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(encodedKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
