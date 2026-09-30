using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Entities;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Sms {
    public class SmsService(CRMContextDb db, FarazSmsService farazSmsService, ISmsIrService smsIrService, DateTimeServices dtService) {
        private const string FarazSmsCode = "cdde93d8-0522-4370-a734-008265e10245";
        private const string SmsIrCode = "c02f8771-cf60-4891-84cb-a21de966b3fc";

        public Task<ResponseSmsModel> SendPatternAsync(SmsJob job)
        {
            return ExecuteAsync(
                () => farazSmsService.SendPatternAsync(job),
                () => smsIrService.SendPatternAsync(job)
            );
        }

        public Task<ResponseSmsModel> SendTextAsync(string[] mobiles, string message)
        {
            return ExecuteAsync(
                () => farazSmsService.SendTextAsync(mobiles, message),
                () => smsIrService.SendTextAsync(mobiles, message)
            );
        }


        private async Task<ResponseSmsModel> ExecuteAsync(Func<Task<ResponseSmsModel>> farazAction, Func<Task<ResponseSmsModel>> smsIrAction)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            if (setting?.SmsServiceCode == null)
            {
                return new ResponseSmsModel { code = 0, Desc = "سرویس پیامک تنظیم نشده است" };
            }

            try
            {
                return setting.SmsServiceCode.ToString() switch {
                    FarazSmsCode => await farazAction(),
                    SmsIrCode => await smsIrAction(),
                    _ => new ResponseSmsModel { code = 0, Desc = "سرویس پیامک نامعتبر است" }
                };
            }
            catch (Exception ex)
            {
                return new ResponseSmsModel { code = 0, Desc = ex.Message };
            }
        }

        public async Task<SmsDashboardModel> GetDashboardAsync()
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting?.SmsServiceCode.ToString() switch {
                FarazSmsCode => await farazSmsService.GetDashboardAsync(),
                SmsIrCode => await smsIrService.GetDashboardAsync(),
                _ => null
            };
        }

        public async Task<SmsReportResult> GetReportsAsync(int page = 1, int pageSize = 100)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting?.SmsServiceCode.ToString() switch {
                FarazSmsCode => await farazSmsService.GetReportsAsync(page, pageSize),
                SmsIrCode => await smsIrService.GetReportsAsync(page, pageSize),
                _ => new SmsReportResult()
            };
        }

        public async Task<SmsReceiveResult> GetReceivesAsync(int page = 1, int pageSize = 100)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting?.SmsServiceCode.ToString() switch {
                FarazSmsCode => await farazSmsService.GetReceivesAsync(page, pageSize),
                SmsIrCode => await smsIrService.GetReceivesAsync(page, pageSize),
                _ => new SmsReceiveResult()
            };
        }


        public async Task SyncReceivesAsync(CancellationToken cancellationToken = default)
        {
            const int pageSize = 100;
            const int overlapHours = 1;

            var lastReceivedDate = await db.ReceivedSmses
                .AsNoTracking()
                .Select(x => (DateTime?)x.CreatedAtFaraz)
                .MaxAsync(cancellationToken);

            var syncFromDate = lastReceivedDate?.AddHours(-overlapHours);

            var page = 1;
            var items = new List<SmsReceiveModel>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await GetReceivesAsync(page, pageSize);

                if (result.Data.Count == 0)
                    break;

                items.AddRange(syncFromDate == null ? result.Data : result.Data.Where(x => x.CreatedAtFaraz >= syncFromDate.Value));

                var oldestItemInPage = result.Data.Min(x => x.CreatedAtFaraz);

                if (syncFromDate != null && oldestItemInPage < syncFromDate.Value)
                    break;

                if (page >= result.TotalPages)
                    break;

                page++;
            }

            if (items.Count == 0)
                return;

            var farazIds = items
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var existingSms = await db.ReceivedSmses
                .Where(x => farazIds.Contains(x.FarazId))
                .ToDictionaryAsync(x => x.FarazId, cancellationToken);

            foreach (var item in items)
            {
                if (existingSms.TryGetValue(item.Id, out var existing))
                {
                    existing.Seen = item.Seen;
                    continue;
                }

                db.ReceivedSmses.Add(new ReceivedSms {
                    FarazId = item.Id,
                    Mobile = item.Mobile,
                    MessageText = item.MessageText,
                    Number = item.Number,
                    ReceivedDateTime = item.ReceivedDateTime,
                    CreatedAtFaraz = item.CreatedAtFaraz,
                    ReceivedDateTimeTxt = dtService.GetDateTimePersian(item.ReceivedDateTime),
                    Seen = item.Seen,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}