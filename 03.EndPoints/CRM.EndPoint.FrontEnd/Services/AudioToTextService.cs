using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.EndPoint.FrontEnd.Models.ViewModel;

namespace CRM.EndPoint.FrontEnd.Services;

public class AudioToTextService
{
    private readonly HttpClient _http;

    public AudioToTextService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AiResponseDetails> ConvertAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();

        // پارامترهای API
        form.Add(new StringContent("default"), "model");
        form.Add(new StringContent("false"), "srt");
        form.Add(new StringContent("false"), "inverseNormalizer");
        form.Add(new StringContent("false"), "timestamp");
        form.Add(new StringContent("false"), "spokenPunctuation");
        form.Add(new StringContent("true"), "punctuation");
        form.Add(new StringContent("1"), "numSpeakers");
        form.Add(new StringContent("false"), "diarize");

        // فایل صوتی
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        form.Add(fileContent, "audio", Path.GetFileName(filePath));

        // ارسال درخواست
        var response = await _http.PostAsync("", form);

        if (response.IsSuccessStatusCode)
        {
            var resJson = await response.Content.ReadFromJsonAsync<AiResponseModel>();
            return resJson.Data.Data.AiResponse;
        }

        throw new Exception("API Error: " + await response.Content.ReadAsStringAsync());

    }
}
