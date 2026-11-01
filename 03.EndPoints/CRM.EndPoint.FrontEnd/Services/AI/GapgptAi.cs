using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Ai;
using CRM.EndPoint.FrontEnd.Models.Supply;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi(HttpClient httpClient, AiPrompts aiPrompts, ILogger<GapgptAi> logger) {
    public async Task<AiGapgptResult<SupplyFactorAiDto>> ProcessInvoiceWithGemini(byte[] fileBytes, string extension, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(fileBytes);
        var mimeType = GetMimeType(extension);

        var prompt = aiPrompts.BuildInvoice();

        var contentParts = new List<object> {
            new { type = "text", text = prompt }
        };

        if (mimeType == "application/pdf")
        {
            contentParts.Add(new { type = "file", file = new { filename = "factor.pdf", file_data = $"data:{mimeType};base64,{base64}" } });
        } else
        {
            contentParts.Add(new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64}" } });
        }

        var requestBody = new {
            model = "gemini-2.5-flash",
            messages = new[] {
                new {
                    role = "user",
                    content = contentParts.ToArray()
                }
            },
            response_format = new { type = "json_object" },
            temperature = 0
        };

        try
        {
           var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody, cancellationToken: ct);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(ct);
                var clean = StripHtml(rawError);

                logger.LogError("Gemini Invoice API Error ({StatusCode}): {Error}", response.StatusCode, clean);

                return new AiGapgptResult<SupplyFactorAiDto> {
                    Success = false,
                    Error = $"خطا در ارتباط با سرویس هوش مصنوعی ({response.StatusCode}). لطفاً کمی بعد دوباره تلاش کنید."
                };
            }

            var resJson = await response.Content.ReadFromJsonAsync<AiGapgptModel>(cancellationToken: ct);

            var content = resJson?.Choices?.FirstOrDefault()?.Message?.Content;

            content = content.Replace("```json", "").Replace("```", "");

            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiGapgptResult<SupplyFactorAiDto> {
                    Success = false,
                    Error = "پاسخی از سرویس هوش مصنوعی دریافت نشد. لطفاً دوباره تلاش کنید."
                };
            }

            SupplyFactorAiDto resultData;

            try
            {
                resultData = JsonSerializer.Deserialize<SupplyFactorAiDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid Gemini invoice JSON response: {Content}", content);
                return new AiGapgptResult<SupplyFactorAiDto> {
                    Success = false,
                    Error = "اطلاعات دریافتی از سرویس هوش مصنوعی قابل پردازش نیست. لطفاً تصویر فاکتور را بررسی کرده و دوباره ارسال کنید."
                };
            }

            if (resultData == null)
            {
                return new AiGapgptResult<SupplyFactorAiDto> {
                    Success = false,
                    Error = "اطلاعاتی از فاکتور استخراج نشد. لطفاً از وضوح و کامل بودن تصویر فاکتور مطمئن شوید."
                };
            }

            logger.LogInformation("Gemini invoice processing completed");

            return new AiGapgptResult<SupplyFactorAiDto> { Success = true, Data = resultData };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return new AiGapgptResult<SupplyFactorAiDto> {
                Success = false,
                Error = "خطای غیرمنتظره‌ای در پردازش فاکتور رخ داد. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید."
            };
        }
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch {
            "pdf" => "application/pdf",
            "jpg" => "image/jpeg",
            "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => throw new NotSupportedException("فرمت فایل پشتیبانی نمی‌شود.")
        };
    }


    public async Task<AiGapgptResult<SummaryModel>> ProcessAudioWithGemini(MemoryStream audio, List<string> userNames, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Gemini Audio Processing.");
            var prompt = AiPrompts.AnalyzeAudioCall(string.Join(", ", userNames));

            var requestBody = new { // https://gapgpt.app/platform-v2/tokens
                model = "gemini-2.5-flash",
//                model = "gemini-3.1-flash-lite",
                messages = new[] {
                    new {
                        role = "user",
                        content = new object[] {
                            new { type = "text", text = prompt },
                            new { type = "input_audio", input_audio = new { data = audio.ToArray(), format = "wav" } }
                        }
                    }
                },
                response_format = new { type = "json_object" },
                temperature = 0,
            };

            var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody, cancellationToken: ct);
            sw.Stop();
            var aiDuration = Math.Round(sw.Elapsed.TotalSeconds, 1);
            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiGapgptModel>(cancellationToken: ct);
                var content = resJson?.Choices?[0]?.Message?.Content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new AiGapgptResult<SummaryModel>
                        { Success = false, Error = "AI returned an empty response.", ElapsedSeconds = aiDuration };
                }

                var resultData = JsonSerializer.Deserialize<SummaryModel>(content);
                logger.LogInformation("Gemini Audio Processing completed in {Elapsed}s.", aiDuration);

                return new AiGapgptResult<SummaryModel> { Success = true, Data = resultData, ElapsedSeconds = aiDuration };
            }

            var rawError = await response.Content.ReadAsStringAsync(ct);
            var clean = StripHtml(rawError);
            logger.LogError("Gemini API Error ({StatusCode}) in {Elapsed}s: {Error}", response.StatusCode, aiDuration, clean);
            return new AiGapgptResult<SummaryModel>
                { Success = false, Error = $"API Error ({response.StatusCode}): {clean}", ElapsedSeconds = aiDuration };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var aiDuration = Math.Round(sw.Elapsed.TotalSeconds, 1);
            logger.LogError(ex, "Error Gemini audio processing after {Elapsed}s.", aiDuration);
            return new AiGapgptResult<SummaryModel> { Success = false, Error = ex.Message, ElapsedSeconds = aiDuration };
        }
    }


    // خلاصه نویسی
    public async Task<AiGapgptResult<SummaryModel>> SummarizeText(string textToSummarize, List<string> userNames, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Summarize started");
            var prompt = aiPrompts.SummarizeCall(textToSummarize, string.Join(", ", userNames));

            var requestBody = new {
                //model = "gapgpt-qwen-3.6",
                //model = "gapgpt-qwen-3.6-thinking",
                model = "gpt-5-mini",
                //model = "gemini-2.5-flash",
                messages = new[] {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.8,
                top_p = 0.7,
            };

            var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody, cancellationToken: ct);
            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiGapgptModel>(cancellationToken: ct);
                logger.LogInformation("Summarize completed");

                var content = resJson?.Choices?[0]?.Message?.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new AiGapgptResult<SummaryModel> { Success = true, Error = "AI returned an empty response." };
                }

                try
                {
                    var summary = JsonSerializer.Deserialize<SummaryModel>(content);
                    return new AiGapgptResult<SummaryModel> { Success = true, Data = summary };
                }
                catch (JsonException ex)
                {
                    return new AiGapgptResult<SummaryModel>
                        { Success = false, Error = $"JSON Deserialize Error: {ex.Message} | Raw Content: {content}" };
                }
            }

            var raw = await response.Content.ReadAsStringAsync(ct);
            var clean = StripHtml(raw);

            logger.LogError("Summarize API Error ({StatusCode}): {Error}", response.StatusCode, clean);
            return new AiGapgptResult<SummaryModel> { Success = false, Error = $"API Error ({response.StatusCode}): {clean}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Summarize error");
            return new AiGapgptResult<SummaryModel> { Success = false, Error = $"Exception: {ex.Message}" };
        }
    }


    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // remove script/style
        input = Regex.Replace(input, "<script.*?</script>", "", RegexOptions.Singleline);
        input = Regex.Replace(input, "<style.*?</style>", "", RegexOptions.Singleline);

        // remove all tags
        input = Regex.Replace(input, "<.*?>", " ");

        // decode HTML entities
        input = System.Net.WebUtility.HtmlDecode(input);

        // normalize spaces
        input = Regex.Replace(input, @"\s+", " ").Trim();

        return input;
    }
}