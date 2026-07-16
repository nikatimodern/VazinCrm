using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.CallRecordings.Enums;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiService(
    CRMContextDb db,
    ViraAi viraAi,
    GapgptAi gapgptAi,
    DateTimeServices dts,
    IssabelService issabelService,
    ILogger<AiService> logger,
    IHubContext<TaskHub> hub) {
    public async Task ProcessCallRecordingGemini(CallRecording call, CancellationToken ct = default)
    {
        if (call == null) return;
        var trackedCall = await GetOrCreateTrackedCall(call, ct);

        try
        {
            await UpdateStatus(trackedCall, AiProcessStatusEnum.DownloadingAudio, ct: ct);
            using var audioStream = await issabelService.GetAudioWithRetry(trackedCall.RecordingFilePath, ct);

            if (audioStream == null)
            {
                await UpdateStatus(trackedCall, AiProcessStatusEnum.DownloadFailed, "Audio download from Issabel failed.", ct);
                return;
            }

            //var audioBase64 = Convert.ToBase64String(audioStream.ToArray());
            await UpdateStatus(trackedCall, AiProcessStatusEnum.Summarizing, ct: ct);

            trackedCall.AiAttempts++;

            var userNames = trackedCall.Participants?.Select(x => x.UserFullName).ToList() ?? [];
            var aiResult = await gapgptAi.ProcessAudioWithGemini(audioStream, userNames, ct);

            trackedCall.AiDurationSeconds = aiResult?.ElapsedSeconds;
            if (!aiResult.Success)
            {
                if (trackedCall.AiAttempts < 3)
                {
                    logger.LogWarning("Transient error for Call ID: {Id}. Waiting for retry. Attempt {Attempt}/3", trackedCall.Id, trackedCall.AiAttempts);
                    await UpdateStatus(trackedCall, AiProcessStatusEnum.WaitingForRetry, $"Transient error: {aiResult.Error}", ct);
                } else
                {
                    await UpdateStatus(trackedCall, AiProcessStatusEnum.SummaryFailed, aiResult.Error, ct);
                }

                return;
            }


            if (aiResult.Data == null || string.IsNullOrWhiteSpace(aiResult.Data.Summary))
            {
                await UpdateStatus(trackedCall, AiProcessStatusEnum.SummaryIgnored, "Summary skipped by AI rules.", ct);
                return;
            }

            trackedCall.SummaryText = aiResult.Data.Summary.Trim();
            trackedCall.Product = aiResult.Data.Product?.Trim();

            if (string.IsNullOrEmpty(trackedCall.CallerCustomerTxt) && !string.IsNullOrEmpty(aiResult.Data.Customer))
            {
                trackedCall.CallerCustomerTxt = aiResult.Data.Customer.Trim();
            }

            trackedCall.AgentReview = aiResult.Data.SalesAgent?.Review;
            trackedCall.AgentScore = aiResult.Data.SalesAgent?.Score;

            await UpdateStatus(trackedCall, AiProcessStatusEnum.SummaryCompleted, ct: ct);

            await CreateReportWithCallData(trackedCall, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in direct Gemini process for call ID: {Id}", trackedCall.Id);
            await UpdateStatus(trackedCall, AiProcessStatusEnum.SystemError, $"Direct Gemini error: {ex.Message}", ct);
        }
    }


    /*    public async Task ProcessCallRecording(CallRecording call, CancellationToken ct)
    {
        if (call == null) return;
        await SaveCallDataToDatabase(call, ct);

        try
        {
            await UpdateStatus(call, AiProcessStatusEnum.DownloadingAudio, ct: ct);

            var audio = await issabelService.GetAudioWithRetry(call.RecordingFilePath, ct);
            if (audio == null)
            {
                await UpdateStatus(call, AiProcessStatusEnum.DownloadFailed, "Audio download failed", ct);
                return;
            }

            await UpdateStatus(call, AiProcessStatusEnum.SendingToAvanegar, ct: ct);
            var result = await viraAi.SendRequestAudio(audio, Path.GetFileName(call.RecordingFilePath), ct);

            if (!result.Success)
            {
                await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, result.Error, ct);
                return;
            }

            call.AvanegarRequestId = result.RequestId;
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarPending, ct: ct);
            logger.LogInformation("Queued to Avanegar. File={File} RequestId={RequestId}", Path.GetFileName(call.RecordingFilePath),
                result.RequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessCallRecording failed. Call={Id}", call.Id);

            await UpdateStatus(call, AiProcessStatusEnum.SystemError, ex.Message, ct);
        }
    }

    public async Task ProcessPendingCall(CallRecording call, CancellationToken ct = default)
    {
        try
        {
            // ۱. پیگیری از آوانگار
            var isTranscriptionReady = await TrackAvanegarAsync(call, ct);
            if (!isTranscriptionReady) return;

            // ۲. خلاصه‌سازی متن
            var isSummaryReady = await SummarizeCallAsync(call, ct);
            if (!isSummaryReady) return;

            // ۳. ساخت گزارش نهایی
            await CreateReportWithCallData(call, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pending call ID: {Id}", call.Id);
            await UpdateStatus(call, AiProcessStatusEnum.SystemError, $"Pending error: {ex.Message}", ct);
        }
    }

    private const int MaxAvanegarAttempts = 60;

    private async Task<bool> TrackAvanegarAsync(CallRecording call, CancellationToken ct)
    {
        if (call.AvanegarAttempts >= MaxAvanegarAttempts)
        {
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, $"Max Track Attempts: {call.AvanegarAttempts}", ct);
            return false;
        }

        call.AvanegarAttempts++;
        await UpdateStatus(call, AiProcessStatusEnum.TrackingAvanegar, ct: ct);

        var result = await viraAi.TrackRequest(call.AvanegarRequestId, ct);

        if (!result.Completed && !result.Failed)
        {
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarPending, ct: ct);
            return false;
        }

        if (result.Failed)
        {
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, result.Error, ct);
            return false;
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            await UpdateStatus(call, AiProcessStatusEnum.AvanegarFailed, "Empty transcription", ct);
            return false;
        }

        call.TranscriptionText = result.Text;
        return true;
    }

    private async Task<bool> SummarizeCallAsync(CallRecording call, CancellationToken ct)
    {
        await UpdateStatus(call, AiProcessStatusEnum.Summarizing, ct: ct);

        var userNames = call.Participants?.Select(x => x.UserFullName).ToList() ?? [];
        var resultSummary = await gapgptAi.SummarizeText(call.TranscriptionText, userNames, ct);

        if (!resultSummary.Success)
        {
            await UpdateStatus(call, AiProcessStatusEnum.SummaryFailed, resultSummary.Error, ct);
            return false;
        }

        if (resultSummary.Data == null || string.IsNullOrWhiteSpace(resultSummary.Data.Summary))
        {
            await UpdateStatus(call, AiProcessStatusEnum.SummaryIgnored, "Summary skipped by AI rules (Null or Empty)", ct);
            return false;
        }

        call.SummaryText = resultSummary.Data.Summary.Trim();
        call.Product = resultSummary.Data.Product?.Trim();

        if (string.IsNullOrEmpty(call.CallerCustomerTxt) && !string.IsNullOrEmpty(resultSummary.Data.Customer))
        {
            call.CallerCustomerTxt = resultSummary.Data.Customer.Trim();
        }

        await UpdateStatus(call, AiProcessStatusEnum.SummaryCompleted, ct: ct);
        return true;
    }*/

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


    private async Task UpdateStatus(CallRecording call, AiProcessStatusEnum status, string error = null, CancellationToken ct = default)
    {
        if (call == null) return;
        call.AiStatus = status;
        call.AiError = error;
        call.ModifyDate = DateTime.Now;
        call.ModifyDateTxt = dts.GetDateTimePersian();

        await db.SaveChangesAsync(ct);
    }

    private async Task<CallRecording> GetOrCreateTrackedCall(CallRecording call, CancellationToken ct)
    {
        try
        {
            if (call == null) return null;

            if (call.Id != Guid.Empty)
            {
                var trackedCall = await db.CallRecordings
                    .Include(x => x.Participants)
                    .FirstOrDefaultAsync(x => x.Id == call.Id, ct);

                if (trackedCall != null)
                {
                    return trackedCall;
                }
            }

            await db.CallRecordings.AddAsync(call, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Call saved: {path}", call.RecordingFilePath);
            return call;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save call failed");
            Console.WriteLine(ex.Message);
            return call;
        }
    }

    private async Task CreateReportWithCallData(CallRecording callRecording, CancellationToken ct)
    {
        if (callRecording == null) return;
        try
        {
            var firstAnswerTime = callRecording.Participants?
                .Where(p => p.AnswerTime != DateTime.MinValue)
                .OrderBy(p => p.AnswerTime)
                .Select(p => p.AnswerTime)
                .FirstOrDefault();

            var reportDate = firstAnswerTime ?? DateTime.Now;

            var newReport = new Report {
                Id = Guid.NewGuid(),
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
            };

            int order = 1;
            foreach (var participant in callRecording.Participants ?? [])
            {
                participant.AnswerTimeTxt = dts.GetDateTimePersian(participant.AnswerTime);
                participant.SortOrder = order++;
            }

            await db.Reports.AddAsync(newReport, ct);

            await UpdateStatus(callRecording, AiProcessStatusEnum.ReportCreated, ct: ct);

            await hub.Clients.All.SendAsync("ShowReport", cancellationToken: ct);
            logger.LogInformation("Report created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report creation failed");
            Console.WriteLine(ex.Message);
            await UpdateStatus(callRecording, AiProcessStatusEnum.ReportFailed, ex.Message, ct);
        }
    }
}