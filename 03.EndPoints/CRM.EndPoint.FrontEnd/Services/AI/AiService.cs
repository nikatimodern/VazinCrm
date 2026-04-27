using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using Generics.Infrastructures.Sql;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiService(CRMContextDb db, ViraAi viraAi, GapgptAi gapgptAi) {
    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    public async Task ProcessCallRecording(string callerNumber, string destinationNumber, string filePath)
    {
        try
        {
            // 1. دانلود فایل صوتی از SFTP
            var audioBytes = await GetAudioFromIsabel(filePath);
            if (audioBytes == null || audioBytes.Length == 0) return;


            // 2. تبدیل صدا به متن
            var transcriptionResult = await viraAi.ConvertAudioToText(audioBytes, Path.GetFileName(filePath));
            var fullTranscriptionText = transcriptionResult?.Result?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(fullTranscriptionText)) return;

            // 3. خلاصه‌سازی متن
            var summary = await gapgptAi.SummarizeText(fullTranscriptionText);

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
}