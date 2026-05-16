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
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiService(CRMContextDb db, ViraAi viraAi, GapgptAi gapgptAi, AiProcessAudio aiProcessAudio, DateTimeServices dts) {
    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    public async Task ProcessCallRecording(CallRecording callData)
    {
        var filePath = @"C:\Users\a.gorji\Desktop\test1.wav";
        try
        {
            // 1. SFTP
            var audioStream = await GetAudioFromIsabel(callData.RecordingFilePath);
            if (audioStream == null) return;
            /*var audioBytes = await File.ReadAllBytesAsync(filePath);
            var audioStream = new MemoryStream(audioBytes);*/

            // 2. Audio To Text
            var transcriptionResult = await viraAi.ConvertAudioToText(audioStream, Path.GetFileName(callData.RecordingFilePath));
            callData.TranscriptionText = transcriptionResult?.Result?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(callData.TranscriptionText)) return;

            // 3. Summary
            var resSummary = await gapgptAi.SummarizeText(callData.TranscriptionText);
            if (resSummary != null)
            {
                callData.SummaryText = resSummary.summary?.Trim();
                if (string.IsNullOrEmpty(callData.CallerCustomerTxt))
                    callData.CallerCustomerTxt = resSummary.customer?.Trim();
            }

            // 4. Save To Database
            await SaveCallDataToDatabase(callData);

            // 4. Create Report
            await CreateReportWithCallData(callData);
            //var result = await aiProcessAudio.ProcessAudioAsync(audioStream, Path.GetFileName(callData.RecordingFilePath));
            //Console.WriteLine(result);
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


    private static async Task<MemoryStream> GetAudioFromIsabel(string remoteFilePath)
    {
        try
        {
            var memoryStream = new MemoryStream();

            using var client = new SftpClient(FtpHost, FtpUser, FtpPass);

            client.Connect();
            client.DownloadFile(remoteFilePath, memoryStream);
            client.Disconnect();

            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }


    private async Task SaveCallDataToDatabase(CallRecording callData)
    {
        try
        {
            await db.CallRecordings.AddAsync(callData);
            await db.SaveChangesAsync();
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
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
            Console.WriteLine(ex.Message);
        }
    }
}