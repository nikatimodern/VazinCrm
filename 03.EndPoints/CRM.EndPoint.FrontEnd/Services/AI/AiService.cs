using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.CallRecordings.Enums;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
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
    ILogger<AiService> logger,
    IHubContext<TaskHub> hub) {
    public async Task ProcessCallRecording(CallRecording call)
    {
        if (call == null) return;
        db.CallRecordings.Add(call);
        await db.SaveChangesAsync();


        try
        {
            await UpdateStatus(call, AiProcessStatusEnum.DownloadingAudio);

            var audio = await issabelService.GetAudioWithRetry(call.RecordingFilePath);
            if (audio == null)
            {
                await UpdateStatus(call, AiProcessStatusEnum.DownloadFailed, "Audio download failed");
                return;
            }

            await UpdateStatus(call, AiProcessStatusEnum.SendingToAvanegar);

            var result = await viraAi.SendRequestAudio(audio, Path.GetFileName(call.RecordingFilePath));

            if (!result.Success)
            {
                await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, result.Error);
                return;
            }

            call.AvanegarRequestId = result.RequestId;
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarPending);
            logger.LogInformation("Queued to Avanegar. File={File} RequestId={RequestId}", Path.GetFileName(call.RecordingFilePath),
                result.RequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessCallRecording failed. Call={Id}", call.Id);

            await UpdateStatus(call, AiProcessStatusEnum.SystemError, ex.Message);
        }
    }

    private async Task UpdateStatus(CallRecording call, AiProcessStatusEnum status, string error = null)
    {
        if (call == null) return;
        call.AiStatus = status;
        call.AiError = error;

        await db.SaveChangesAsync();
    }

    public async Task ProcessPendingCalls(CancellationToken ct)
    {
        var calls = await db.CallRecordings
            .Include(x => x.CallParticipant)
            .Where(x => x.AiStatus == AiProcessStatusEnum.AvanegarPending)
            .OrderBy(x => x.CreateDate)
            .Take(10)
            .ToListAsync(ct);

        foreach (var call in calls)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessPendingCall(call);
        }
    }

    private async Task ProcessPendingCall(CallRecording call)
    {
        try
        {
            call.AiStatus = AiProcessStatusEnum.TrackingAvanegar;
            call.AvanegarAttempts++;
            call.LastTrackDate = DateTime.Now;
            await db.SaveChangesAsync();

            var result = await viraAi.TrackRequest(call.AvanegarRequestId);

            if (!result.Completed && !result.Failed)
            {
                await UpdateStatus(call, AiProcessStatusEnum.AvanegarPending);
                return;
            }

            if (result.Failed)
            {
                await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, result.Error);
                return;
            }

            if (result.Completed)
            {
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, "Empty transcription");
                    return;
                }

                call.TranscriptionText = result.Text;
                await UpdateStatus(call, AiProcessStatusEnum.Summarizing);

                var userNames = call.CallParticipant?.Select(x => x.UserFullName).ToList() ?? [];
                var resultSummary = await gapgptAi.SummarizeText(call.TranscriptionText, userNames);
                if (!resultSummary.Success)
                {
                    await UpdateStatus(call, AiProcessStatusEnum.SummaryFailed, resultSummary.Error);
                    return;
                }

                if (resultSummary.Data == null || string.IsNullOrWhiteSpace(resultSummary.Data.summary))
                {
                    await UpdateStatus(call, AiProcessStatusEnum.SummaryIgnored, "Summary skipped by AI rules (Null or Empty)");
                    return;
                }

                call.SummaryText = resultSummary.Data.summary.Trim();
                call.Product = resultSummary.Data.product?.Trim();
                if (string.IsNullOrEmpty(call.CallerCustomerTxt) && !string.IsNullOrEmpty(resultSummary.Data.customer))
                {
                    call.CallerCustomerTxt = resultSummary.Data.customer.Trim();
                }

                await UpdateStatus(call, AiProcessStatusEnum.SummaryCompleted);

                await CreateReportWithCallData(call);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pending call ID: {Id}", call.Id);
            await UpdateStatus(call, AiProcessStatusEnum.SystemError, $"Pending error: {ex.Message}");
        }
    }


/*
    public async Task ProcessCallRecording(CallRecording callData)
    {
        var filePath = @"C:\Users\a.gorji\Desktop\test1.wav";
        try
        {
            // 1. SFTP
            var audioStream = await issabelService.GetAudioWithRetry(callData.RecordingFilePath);
            if (audioStream == null) return;
            /*var audioBytes = await File.ReadAllBytesAsync(filePath);
            var audioStream = new MemoryStream(audioBytes);#1#

            // 2. Audio To Text
            var audioFileName = Path.GetFileName(callData.RecordingFilePath);
            var transcriptionText = await viraAi.ConvertAudioToText(audioStream, audioFileName, callData.DurationSeconds);
            callData.TranscriptionText = transcriptionText?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(callData.TranscriptionText))
            {
                logger.LogWarning($"TranscriptionText is null or empty: {callData.TranscriptionText}");
                return;
            }

            // 3. Summary
            var resSummary = await gapgptAi.SummarizeText(callData.TranscriptionText, callData.CallParticipant.Select(f => f?.UserFullName).ToList());
            if (resSummary != null)
            {
                callData.SummaryText = resSummary.summary?.Trim();
                callData.Product = resSummary.product?.Trim();
                if (string.IsNullOrEmpty(callData.CallerCustomerTxt))
                    callData.CallerCustomerTxt = resSummary.customer?.Trim();
            }

            // 4. Save To Database
            await SaveCallDataToDatabase(callData);
            if (string.IsNullOrEmpty(callData.SummaryText))
            {
                logger.LogWarning($"Summarize is null or empty: {callData.SummaryText}");
                return;
            }

            // 4. Create Report
            await CreateReportWithCallData(callData);
            //var result = await aiProcessAudio.ProcessAudioAsync(audioStream, Path.GetFileName(callData.RecordingFilePath));
            //Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
*/


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
            var firstAnswerTime = callRecording.CallParticipant?
                .Where(p => p.AnswerTime != null && p.AnswerTime != DateTime.MinValue)
                .OrderBy(p => p.AnswerTime)
                .Select(p => p.AnswerTime)
                .FirstOrDefault();

            var reportDate = firstAnswerTime ?? DateTime.Now;

            var newReport = new Report {
                RepTel = callRecording.CallerNumber,
                UserCreatorID = callRecording.DestinationUserId,
                UserCreator = callRecording.DestinationUserTxt,
                CustomerId = callRecording.CallerCustomerId,
                CustomerTxt = callRecording.CallerCustomerTxt,
                CustomerCode = callRecording.CallerCustomerCode,
                ReportText = callRecording.SummaryText,
                RegDate = reportDate,
                RegDateTxt = dts.GetDatePersian(reportDate),
                RegTimeTxt = reportDate.ToString("HH:mm:ss"),
                ReportType = ReportTypeEnum.گزارش_تماس_های_ورودی,
                ReportTypeTxt = nameof(ReportTypeEnum.گزارش_تماس_های_ورودی).Replace("_", " "),
                AI = true,
                CallRecordingId = callRecording.Id,
                Participants = new List<ReportParticipant>()
            };

            int order = 1;
            foreach (var participant in callRecording.CallParticipant ?? [])
            {
                newReport.Participants.Add(new ReportParticipant {
                    Extension = participant.Extension,
                    UserId = participant.UserId,
                    UserFullName = participant.UserFullName,
                    AnswerTime = participant.AnswerTime,
                    AnswerTimeTxt = dts.GetDateTimePersian(participant.AnswerTime),
                    SortOrder = order++
                });
            }

            await db.Reports.AddAsync(newReport);

            await UpdateStatus(callRecording, AiProcessStatusEnum.ReportCreated);

            await hub.Clients.All.SendAsync("ShowReport");
            logger.LogInformation("Report created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report creation failed");
            Console.WriteLine(ex.Message);
            await UpdateStatus(callRecording, AiProcessStatusEnum.ReportFailed, ex.Message);
        }
    }
}