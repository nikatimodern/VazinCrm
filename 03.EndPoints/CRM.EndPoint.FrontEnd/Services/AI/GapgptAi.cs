using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Ai;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi(HttpClient httpClient, AiPrompts aiPrompts, ILogger<GapgptAi> logger) {
    public async Task<SummaryResult> ProcessAudioWithGemini(MemoryStream audio, List<string> userNames, CancellationToken ct = default)
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
                    return new SummaryResult { Success = false, Error = "AI returned an empty response.", ElapsedSeconds = aiDuration };
                }

                var resultData = JsonSerializer.Deserialize<SummaryModel>(content);
                logger.LogInformation("Gemini Audio Processing completed in {Elapsed}s.", aiDuration);

                return new SummaryResult { Success = true, Data = resultData, ElapsedSeconds = aiDuration };
            }

            var rawError = await response.Content.ReadAsStringAsync(ct);
            var clean = StripHtml(rawError);
            logger.LogError("Gemini API Error ({StatusCode}) in {Elapsed}s: {Error}", response.StatusCode, aiDuration, clean);
            return new SummaryResult { Success = false, Error = $"API Error ({response.StatusCode}): {clean}", ElapsedSeconds = aiDuration };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var aiDuration = Math.Round(sw.Elapsed.TotalSeconds, 1);
            logger.LogError(ex, "Error Gemini audio processing after {Elapsed}s.", aiDuration);
            return new SummaryResult { Success = false, Error = ex.Message, ElapsedSeconds = aiDuration };
        }
    }


    // خلاصه نویسی
    public async Task<SummaryResult> SummarizeText(string textToSummarize, List<string> userNames, CancellationToken ct = default)
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