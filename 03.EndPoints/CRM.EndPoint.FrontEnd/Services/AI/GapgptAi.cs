using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi {
    private readonly HttpClient _httpClient;
    private readonly AiPrompts _aiPrompts;
    private readonly ILogger<GapgptAi> logger;

    public GapgptAi(HttpClient httpClient, AiPrompts aiPrompts, ILogger<GapgptAi> logger)
    {
        _httpClient = httpClient;
        _aiPrompts = aiPrompts;
        this.logger = logger;
    }

    // خلاصه نویسی
    public async Task<SummaryModel> SummarizeText(string textToSummarize, string userName) // https://gapgpt.app/platform-v2/tokens
    {
        try
        {
            logger.LogInformation("Summarize started");
            var prompt = _aiPrompts.SummarizeCall(textToSummarize, userName);

            var requestBody = new {
                //model = "gapgpt-qwen-3.6",
                //model = "gapgpt-qwen-3.6-thinking",
                //model= "gpt-5-mini",
                model= "gemini-2.5-flash",
                messages = new[] {
                    new { role = "user", content = prompt }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiGapgptModel>();
                logger.LogInformation("Summarize completed");

                var content = resJson?.Choices?[0]?.Message?.Content;
                var summary = JsonSerializer.Deserialize<SummaryModel>(content);
                return summary;
            } else
            {
                var raw = await response.Content.ReadAsStringAsync();
/*

                var clean = response.Content.Headers.ContentType?.MediaType == "text/html"
                    ? ExtractImportantError(raw)
                    : raw;*/

                var clean = StripHtml(raw);
                var clean2 = ExtractImportantError(raw);

                logger.LogError("Summarize API Error ({StatusCode}): {Error}", response.StatusCode, clean);
                /*var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Summarize API Error ({ResponseStatusCode}): {ErrorContent}", response.StatusCode, errorContent);*/
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Summarize error");
            return null;
        }
    }

    private static string ExtractImportantError(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = StripHtml(html);

        // فقط قسمت مهم
        var match = Regex.Match(text, @"Error\s\d+.*?Unavailable!", RegexOptions.IgnoreCase);

        if (match.Success)
            return match.Value;

        return text.Length > 300
            ? text[..300]
            : text;
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