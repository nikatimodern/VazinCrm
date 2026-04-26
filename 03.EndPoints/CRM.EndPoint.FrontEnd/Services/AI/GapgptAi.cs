using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class GapgptAi {
    private readonly IHttpClientFactory _httpClientFactory;

    public GapgptAi(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private async Task<string> SummarizeText(string textToSummarize) // خلاصه نویسی
    {
        var httpClient = _httpClientFactory.CreateClient("GapgptApi");

        var prompt = $@"
            شما یک دستیار داخلی برای تیم فروش شرکت «وزین پلاست» هستید.

            زمینه:
            شرکت در حوزه تولید نایلون، نایلکس و محصولات پلی‌اتیلنی فعالیت می‌کند.

            متنی که دریافت می‌کنید:
            خروجی تبدیل گفتار به متن از مکالمه تلفنی بین مشتری و کارشناس فروش است
            و ممکن است دارای اشتباه، تکرار یا جملات ناقص باشد.

            هدف:
            یک خلاصه کوتاه، کاربردی و رسمی و قابل استفاده برای تیم فروش تولید کن.

            قوانین مهم:
            - از اضافه‌گویی خودداری کن
            - از حدس زدن یا تغییر اعداد خودداری کن
            - فقط اطلاعاتی که در متن هست را استخراج کن
            متن:
            {textToSummarize}
        ";

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