using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiPrompts _aiPrompts;

    public GapgptAi(IHttpClientFactory httpClientFactory, AiPrompts aiPrompts)
    {
        _httpClientFactory = httpClientFactory;
        _aiPrompts = aiPrompts;
    }

    public async Task<string> SummarizeText(string textToSummarize) // خلاصه نویسی
    {
        var httpClient = _httpClientFactory.CreateClient("GapgptApi");

        var prompt = _aiPrompts.SummarizeCall(textToSummarize);

        var requestBody = new {
//            model = "gapgpt-qwen-3.6",
            model = "gapgpt-qwen-3.6-thinking",
            messages = new[] {
                new { role = "user", content = prompt }
            }
        };

        var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AiGapgptModel>();

        return result?.Choices?[0]?.Message?.Content;

    }
}