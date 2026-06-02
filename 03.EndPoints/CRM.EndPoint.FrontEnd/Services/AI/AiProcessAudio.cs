using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiProcessAudio {
    private readonly HttpClient _httpClient;

    public AiProcessAudio(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiAudioAnalysisModel> ProcessAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (audioStream == null)
            throw new ArgumentNullException(nameof(audioStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        try
        {
            if (audioStream.CanSeek)
                audioStream.Position = 0;

            using var formData = new MultipartFormDataContent();

            using var fileContent = new StreamContent(audioStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mpeg");

            formData.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync("process-audio", formData, cancellationToken);

            var resJson = await response.Content.ReadFromJsonAsync<AiAudioAnalysisModel>(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"AI API ERROR\nStatusCode: {(int)response.StatusCode}\nReason: {response.ReasonPhrase}\n\nResponse:\n{resJson}"
                );

                return null;
            }

            return resJson;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI APIError: {ex.Message}");
            return null;
        }
    }
}