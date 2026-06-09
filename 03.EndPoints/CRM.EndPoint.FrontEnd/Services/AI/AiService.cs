using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
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
    IssabelService issabelService,
    ILogger<AiService> logger) {
    public async Task ProcessCallRecording(CallRecording callData)
    {
        var filePath = @"C:\Users\a.gorji\Desktop\test1.wav";
        try
        {
            // 1. SFTP
            var audioStream = await issabelService.GetAudioWithRetry(callData.RecordingFilePath);
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
            }

            // 3. Summary
            var resSummary = await gapgptAi.SummarizeText(callData.TranscriptionText, callData.DestinationUserTxt);
            if (resSummary != null)
            {
                callData.SummaryText = resSummary.summary?.Trim();
                callData.Product = resSummary.product?.Trim();
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
            }

            await CreateReportWithCallData(callData);
            //var result = await aiProcessAudio.ProcessAudioAsync(audioStream, Path.GetFileName(callData.RecordingFilePath));
            //Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private async Task SaveCallDataToDatabase(CallRecording callData)
    {
        try
        {

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
        if (callRecording == null) return;
        try
        {

            var newReport = new Report {
                RepTel = callRecording.CallerNumber,
                UserCreatorID = callRecording.DestinationUserId,
                UserCreator = callRecording.DestinationUserTxt,
                CustomerId = callRecording.CallerCustomerId,
                CustomerTxt = callRecording.CallerCustomerTxt,
                CustomerCode = callRecording.CallerCustomerCode,
                ReportText = callRecording.SummaryText + " **",
                RegDate = DateTime.Now,
                RegDateTxt = dts.GetDatePersian(),
                RegTimeTxt = dts.GetNowTime("HH:mm:ss"),
                ReportType = ReportTypeEnum.گزارش_تماس_های_ورودی,
                ReportTypeTxt = nameof(ReportTypeEnum.گزارش_تماس_های_ورودی).Replace("_", " "),
                AI = true,
                CallRecordingId = callRecording.Id,
            };

            int order = 1;
            foreach (var participant in callRecording.CallParticipant ?? [])
            {
                newReport.Participants?.Add(new ReportParticipant {
                    Extension = participant.Extension,
                    UserId = participant.UserId,
                    UserFullName = participant.UserFullName,
                    AnswerTime = participant.AnswerTime,
                    SortOrder = order++
                });
            }

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