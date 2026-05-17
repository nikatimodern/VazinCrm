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

            if (string.IsNullOrEmpty(callData.TranscriptionText))
            {
                logger.LogWarning($"TranscriptionText is null or empty: {callData.TranscriptionText}");
                return;
            };

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
            if (string.IsNullOrEmpty(callData.SummaryText))
            {
                logger.LogWarning($"Summarize is null or empty: {callData.SummaryText}");
                return;
            };

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


    private async Task<MemoryStream> GetAudioFromIsabel(string remoteFilePath)
    {
        logger.LogInformation("Downloading audio: {Path}", remoteFilePath);
        var memoryStream = new MemoryStream();

        using var client = new SftpClient(FtpHost, FtpUser, FtpPass);

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        client.OperationTimeout = TimeSpan.FromSeconds(60);

        client.Connect();
        logger.LogInformation("SFTP connected");

        client.DownloadFile(remoteFilePath, memoryStream);
        logger.LogInformation("Audio downloaded with size: {Size}", memoryStream.Length);
        client.Disconnect();
        logger.LogInformation("SFTP disconnected");

        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<MemoryStream> GetAudioWithRetry(string path)
    {
        try
        {
            logger.LogInformation("Audio retry started");
            for (var i = 0; i < 5; i++)
            {
                logger.LogInformation("Audio attempt {Attempt}/5", i + 1);
                try
                {
                    var stream = await GetAudioFromIsabel(path);

                    if (stream != null && stream.Length > 0)
                    {
                        logger.LogInformation("Audio loaded");
                        return stream;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Audio retry failed");
                    Console.WriteLine(ex);
                }

                await Task.Delay(2000);
            }

            logger.LogWarning("Audio retries exhausted");
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Audio load error");
            Console.WriteLine(e);
            return null;
        }
    }


    private async Task SaveCallDataToDatabase(CallRecording callData)
    {
        try
        {
            logger.LogInformation("Saving call");
            await db.CallRecordings.AddAsync(callData);
            await db.SaveChangesAsync();
            logger.LogInformation("Call saved: {path}", callData.RecordingFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save call failed");
            Console.WriteLine(ex.Message);
        }
    }

    private async Task CreateReportWithCallData(CallRecording callRecording)
    {
        logger.LogInformation("Report creation started");
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

            logger.LogInformation("Saving report");
            await db.Reports.AddAsync(newReport);

            await db.SaveChangesAsync();
            logger.LogInformation("Report created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report creation failed");
            Console.WriteLine(ex.Message);
        }
    }
}