using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class ViraAi {
    private readonly IHttpClientFactory _httpClientFactory;

    public ViraAi(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }



    public async Task<AiResponseDetails> ConvertAudioToText(Stream audioStream, string audioFileName)
    {
        var httpClient = _httpClientFactory.CreateClient("AvanegarApi");

        using var form = new MultipartFormDataContent();

        // API Parameters
        form.Add(new StringContent("default"), "model"); // https://api.ivira.ai/partai/avanegar?type=document
        form.Add(new StringContent("false"), "srt");
        form.Add(new StringContent("true"), "inverseNormalizer");
        form.Add(new StringContent("false"), "timestamp");
        form.Add(new StringContent("false"), "spokenPunctuation");
        form.Add(new StringContent("true"), "punctuation");
        form.Add(new StringContent("2"), "numSpeakers");
        form.Add(new StringContent("true"), "diarize");

        // فایل صوتی
        //var filePath = Path.Combine("wwwroot", "a.mp3");
        //var fileBytess = await File.ReadAllBytesAsync(filePath);

        var fileContent = new StreamContent(audioStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        form.Add(fileContent, "audio", audioFileName);


        try
        {
            var response = await httpClient.PostAsync("", form);

            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiAvanegarModel>();
                return resJson.Data.Data.AiResponse;
            } else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Transcription API Error ({response.StatusCode}): {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during transcription API call: {ex.Message}");
            return null;
        }
    }
}