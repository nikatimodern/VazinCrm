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
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiService(CRMContextDb db, ViraAi viraAi, GapgptAi gapgptAi, DateTimeServices dts) {
    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";

    public async Task ProcessCallRecording(CallRecording callData)
    {
        try
        {
            // 1. دانلود فایل صوتی از SFTP
            var audioBytes = await GetAudioFromIsabel(callData.RecordingFilePath);
            if (audioBytes == null || audioBytes.Length == 0) return;


            // 2. تبدیل صدا به متن
            var transcriptionResult = await viraAi.ConvertAudioToText(audioBytes, Path.GetFileName(callData.RecordingFilePath));
            callData.TranscriptionText = transcriptionResult?.Result?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(callData.TranscriptionText)) return;

            // 3. خلاصه‌سازی متن
            callData.SummaryText = await gapgptAi.SummarizeText(callData.TranscriptionText);

            // 4. ذخیره اطلاعات در پایگاه داده
            await SaveCallDataToDatabase(callData);

            // 4. ثبت گزارش کار
            await CreateReportWithCallData(callData);
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


    private async Task<CallRecording> SaveCallDataToDatabase(CallRecording callData)
    {
        try
        {
            await db.CallRecordings.AddAsync(callData);
            await db.SaveChangesAsync();
            return callData;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
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
                ReportTypeTxt = nameof(ReportTypeEnum.گزارش_تماس_های_ورودی).Replace("_", " ")
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