using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using Generics.Infrastructures.Sql;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AudioToTextService {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly CRMContextDb db;

    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";
    //private const string TargetFilePath = @"wwwroot\recordings";

    public AudioToTextService(IHttpClientFactory httpClientFactory, HttpClient httpClient, CRMContextDb db)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClient;
        this.db = db;
    }


    private static async Task<byte[]> GetAudioFromIsabel(string remoteFilePath)
    {
        try
        {
            using var client = new SftpClient(FtpHost, FtpUser, FtpPass);
            client.Connect();

            await using var memoryStream = new MemoryStream();
            client.DownloadFile(remoteFilePath, memoryStream);


            client.Disconnect();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }


    private async Task<AiResponseDetails> ConvertAudioToText(byte[] fileBytes, string audioFileName)
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
        form.Add(new StringContent("0"), "numSpeakers");
        form.Add(new StringContent("true"), "diarize");

        // فایل صوتی
        //var filePath = Path.Combine("wwwroot", "a.mp3");
        //var fileBytess = await File.ReadAllBytesAsync(filePath);

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        form.Add(fileContent, "audio", audioFileName);


        try
        {
            var response = await httpClient.PostAsync("", form);

            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<AiResponseModel>();
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


    private async Task<string> SummarizeText(string textToSummarize)
    {
        var httpClient = _httpClientFactory.CreateClient("SummarizeApi");

        var requestData = new {
            input = textToSummarize,
        };


        try
        {
            var jsonContent = JsonSerializer.Serialize(requestData);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("", httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();

                // var summaryResult = System.Text.Json.JsonSerializer.Deserialize<YourSummaryResponseModel>(responseJson);
                // return summaryResult.Summary; // یا هر فیلد مربوطه

                var simplifiedResponse = JsonDocument.Parse(responseJson).RootElement.GetProperty("data").GetString();
                return simplifiedResponse;
            } else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Summarization API Error ({response.StatusCode}): {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during summarization API call: {ex.Message}");
            return null;
        }
    }

    private async Task SaveCallDataToDatabase(string filePath, string callerNumber, string destinationNumber, string fullTranscription,
        string summary)
    {
        try
        {
            var model = new CallRecording {
                CallerNumber = callerNumber,
                DestinationNumber = destinationNumber,
                TranscriptionText = fullTranscription,
                SummaryText = summary,
                RecordingFilePath = filePath,
            };

            await db.AddAsync(model);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task ProcessCallRecording(string callerNumber, string destinationNumber, string filePath)
    {
        try
        {
            // 1. دانلود فایل صوتی از SFTP
            var audioBytes = await GetAudioFromIsabel(filePath);
            if (audioBytes == null || audioBytes.Length == 0) return;


            // 2. تبدیل صدا به متن
            var transcriptionResult = await ConvertAudioToText(audioBytes, Path.GetFileName(filePath));
            var fullTranscriptionText = transcriptionResult?.Result?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(fullTranscriptionText)) return;

            // 3. خلاصه‌سازی متن
            string summary = await SummarizeText(fullTranscriptionText);

            // 4. ذخیره اطلاعات در پایگاه داده
            await SaveCallDataToDatabase(filePath, callerNumber, destinationNumber, fullTranscriptionText, summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            GC.Collect();
        }
    }
}