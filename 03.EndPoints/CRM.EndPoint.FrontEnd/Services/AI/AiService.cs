using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiService(
    CRMContextDb db,
    ViraAi viraAi,
    GapgptAi gapgptAi,
    AiProcessAudio aiProcessAudio,
    DateTimeServices dts,
    ILogger<AiService> logger) {

    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    public async Task ProcessCallRecording(CallRecording callData)
    {
        var filePath = @"C:\Users\a.gorji\Desktop\test1.wav";
        try
        {
            // 1. SFTP
            var audioStream = await GetAudioWithRetry(callData.RecordingFilePath);
            if (audioStream == null) return;
            /*var audioBytes = await File.ReadAllBytesAsync(filePath);
            var audioStream = new MemoryStream(audioBytes);*/

            // 2. Audio To Text
            var transcriptionResult = await viraAi.ConvertAudioToText(audioStream, Path.GetFileName(callData.RecordingFilePath));
            callData.TranscriptionText = transcriptionResult?.Result?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(callData.TranscriptionText)) return;

            // 3. Summary
            var resSummary = await gapgptAi.SummarizeText(callData.TranscriptionText, callData.DestinationUserTxt);
            if (resSummary != null)
            {
                callData.SummaryText = resSummary.summary?.Trim();
                if (string.IsNullOrEmpty(callData.CallerCustomerTxt))
                    callData.CallerCustomerTxt = resSummary.customer?.Trim();
            }

            // 4. Save To Database
            await SaveCallDataToDatabase(callData);

            // 4. Create Report
            if (string.IsNullOrEmpty(callData.SummaryText)) return;

            await CreateReportWithCallData(callData);
            //var result = await aiProcessAudio.ProcessAudioAsync(audioStream, Path.GetFileName(callData.RecordingFilePath));
            //Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            Console.WriteLine(ex.Message);
        }
    }


    private static async Task<MemoryStream> GetAudioFromIsabel(string remoteFilePath)
    {
        var memoryStream = new MemoryStream();

        using var client = new SftpClient(FtpHost, FtpUser, FtpPass);

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        client.OperationTimeout = TimeSpan.FromSeconds(60);

        client.Connect();
        client.DownloadFile(remoteFilePath, memoryStream);
        client.Disconnect();

        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<MemoryStream> GetAudioWithRetry(string path)
    {
        try
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    var stream = await GetAudioFromIsabel(path);

                    if (stream != null && stream.Length > 0)
                        return stream;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(2000);
            }

            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            Console.WriteLine(e);
            return null;
        }
    }


    private async Task SaveCallDataToDatabase(CallRecording callData)
    {
        try
        {
            await db.CallRecordings.AddAsync(callData);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            Console.WriteLine(ex.Message);
        }
    }

    private async Task CreateReportWithCallData(CallRecording callRecording)
    {
        try
        {
            int? customerCode = null;
            if (callRecording.CallerCustomerId.HasValue)
            {
                customerCode = await db.Customers
                    .Where(f => f.Id == callRecording.CallerCustomerId)
                    .Select(f => f.CustomerCode)
                    .FirstOrDefaultAsync();
            }

            var newReport = new Report {
                RepTel = callRecording.CallerNumber,
                UserCreatorID = callRecording.DestinationUserId,
                UserCreator = callRecording.DestinationUserTxt,
                CustomerId = callRecording.CallerCustomerId,
                CustomerTxt = callRecording.CallerCustomerTxt,
                CustomerCode = customerCode,
                ReportText = callRecording.SummaryText,
                RegDate = DateTime.Now,
                RegDateTxt = dts.GetDatePersian(),
                RegTimeTxt = dts.GetNowTime("HH:mm:ss"),
                ReportType = ReportTypeEnum.گزارش_تماس_های_ورودی,
                ReportTypeTxt = nameof(ReportTypeEnum.گزارش_تماس_های_ورودی).Replace("_", " "),
                AI = true
            };

            await db.Reports.AddAsync(newReport);

            //await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            Console.WriteLine(ex.Message);
        }
    }
}