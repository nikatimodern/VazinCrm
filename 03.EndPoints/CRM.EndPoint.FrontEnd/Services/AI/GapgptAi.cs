using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
                model = "gapgpt-qwen-3.6-thinking",
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
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError($"Summarize API Error ({response.StatusCode}): {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Summarize error");
            return null;
        }
    }
}