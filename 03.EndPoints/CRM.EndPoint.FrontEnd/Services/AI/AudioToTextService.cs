using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Generics.Infrastructures.Sql;
using OpenAI;
using OpenAI.Models;
using OpenAI.Responses;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AudioToTextService {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CRMContextDb db;

    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";
    //private const string TargetFilePath = @"wwwroot\recordings";

    public AudioToTextService(IHttpClientFactory httpClientFactory, CRMContextDb db)
    {
        _httpClientFactory = httpClientFactory;
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




    private async Task<string> SummarizeText(string textToSummarize)
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

    private async Task SaveCallDataToDatabase(string filePath, string callerNumber, string destNumber, string fullTranscription, string summary)
    {
        try
        {
            var model = new CallRecording {
                CallerNumber = callerNumber,
                DestinationNumber = destNumber,
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
            var summary = await SummarizeText(fullTranscriptionText);

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