using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi(HttpClient httpClient, AiPrompts aiPrompts, ILogger<GapgptAi> logger) {
    public async Task<SummaryResult> ProcessAudioWithGemini(string audioBase64, List<string> userNames, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Gemini Audio Processing started.");
            var prompt = aiPrompts.AnalyzeAudioCall(string.Join(", ", userNames));

            var requestBody = new {
                model = "gemini-2.5-flash",
                messages = new[] {
                    new {
                        role = "user",
                        content = new object[] {
                            new { type = "text", text = prompt },
                            new { type = "input_audio", input_audio = new { data = audioBase64, format = "wav" } }
                        }
                    }
                },
                response_format = new { type = "json_object" },
                temperature = 0.8,
                top_p = 0.7
            };

            var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody, cancellationToken: ct);

            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiGapgptModel>(cancellationToken: ct);
                var content = resJson?.Choices?[0]?.Message?.Content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new SummaryResult { Success = false, Error = "AI returned an empty response." };
                }

                var resultData = JsonSerializer.Deserialize<SummaryModel>(content);
                logger.LogInformation("Gemini Audio Processing completed.");

                return new SummaryResult { Success = true, Data = resultData };
            }

            var rawError = await response.Content.ReadAsStringAsync(ct);
            var clean = StripHtml(rawError);
            logger.LogError("Gemini API Error ({StatusCode}): {Error}", response.StatusCode, clean);
            return new SummaryResult { Success = false, Error = $"API Error ({response.StatusCode}): {clean}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Gemini audio processing.");
            return new SummaryResult { Success = false, Error = ex.Message };
        }
    }


    // خلاصه نویسی
    public async Task<SummaryResult>
        SummarizeText(string textToSummarize, List<string> userNames, CancellationToken ct = default) // https://gapgpt.app/platform-v2/tokens
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
                    return new SummaryResult { Success = true, Error = "AI returned an empty response." };
                }

                try
                {
                    var summary = JsonSerializer.Deserialize<SummaryModel>(content);
                    return new SummaryResult { Success = true, Data = summary };
                }
                catch (JsonException ex)
                {
                    return new SummaryResult { Success = false, Error = $"JSON Deserialize Error: {ex.Message} | Raw Content: {content}" };
                }
            }

            var raw = await response.Content.ReadAsStringAsync(ct);
            var clean = StripHtml(raw);

            logger.LogError("Summarize API Error ({StatusCode}): {Error}", response.StatusCode, clean);
            return new SummaryResult { Success = false, Error = $"API Error ({response.StatusCode}): {clean}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Summarize error");
            return new SummaryResult { Success = false, Error = $"Exception: {ex.Message}" };
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