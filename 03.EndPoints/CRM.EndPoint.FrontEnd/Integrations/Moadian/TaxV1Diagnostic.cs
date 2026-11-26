using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxConnectionCheck;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal static class TaxV1Diagnostic
{
    private const string Mask = "[REDACTED]";

    internal static string ReadRequestShape(HttpContent content) => content is JsonContent json
        ? JsonSerializer.Serialize(Redact(JsonSerializer.SerializeToElement(json.Value, JsonSerializerOptions.Web)))
        : null;

    internal static TaxConnectionDiagnostic Describe(Uri baseAddress, HttpRequestMessage request, string requestJson,
        int? statusCode, string contentType, string errorCode)
    {
        var target = request.RequestUri.IsAbsoluteUri ? request.RequestUri : new Uri(baseAddress, request.RequestUri);
        var url = target.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        var names = request.Headers.Select(header => header.Key).Concat(request.Content.Headers.Select(header => header.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        return new(url, names, requestJson, statusCode, contentType, errorCode, DescribeError(errorCode));
    }

    internal static string SafeContentType(MediaTypeHeaderValue contentType)
    {
        var media = contentType?.MediaType?.ToLowerInvariant();
        var safeMedia = media switch
        {
            null => null,
            "application/json" or "application/problem+json" or "application/octet-stream" or "application/xml" or
            "text/plain" or "text/html" or "text/xml" => media,
            _ => Mask
        };
        if (safeMedia is null or Mask || contentType.Parameters.Count == 0) return safeMedia;
        var charset = contentType.CharSet?.Trim('"').ToLowerInvariant();
        var safeCharset = charset is "utf-8" or "utf-16" or "us-ascii" or "iso-8859-1" ? charset : Mask;
        var charsetPart = charset is null ? "" : "; charset=" + safeCharset;
        var otherParameters = contentType.Parameters.Any(parameter =>
            !parameter.Name.Equals("charset", StringComparison.OrdinalIgnoreCase));
        return safeMedia + charsetPart + (otherParameters ? "; " + Mask : "");
    }

    private static object Redact(JsonElement value, string name = null) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(field => field.Name, field => Redact(field.Value, field.Name)),
        JsonValueKind.Array => value.EnumerateArray().Select(item => Redact(item)).ToArray(),
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String when value.GetString() == "" => "",
        JsonValueKind.String when name == "packetType" &&
            value.GetString() is "GET_TOKEN" or "GET_FISCAL_INFORMATION" or "GET_SERVER_INFORMATION" => value.GetString(),
        _ => Mask
    };

    // Only fixed, known authority descriptions may reach the admin response.
    private static string DescribeError(string code) => code switch
    {
        null => null,
        "5000" => "خطای داخلی سامانه",
        "5001" => "درخواست نامعتبر",
        "5002" => "درخواست مجوز لازم را ندارد",
        "5003" => "قالب شناسه بسته نامعتبر است",
        "5004" => "ساختار JSON درخواست نامعتبر است",
        "5005" => "شناسه بسته تکراری است",
        "5006" => "تعداد بسته‌ها بیش از حد مجاز است",
        "5007" => "نوع بسته پشتیبانی نمی‌شود",
        "5008" => "شناسه کلید رمزنگاری نامعتبر است",
        "5009" => "نوع بسته با عملیات همگام همخوانی ندارد",
        "5010" => "زمان اعتبار درخواست گذشته است",
        "5011" => "شناسه رهگیری درخواست تکراری است",
        "5012" => "حافظه مالیاتی پیدا نشد",
        "5013" => "امضای درخواست نامعتبر است",
        "5014" => "قالب درخواست نامعتبر است",
        "5015" => "توکن نامعتبر است",
        _ => "شرح قابل نمایش برای این کد در دسترس نیست"
    };
}
