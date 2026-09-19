using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Sms {
    public class SmsService(CRMContextDb db, FarazSmsService farazSmsService, ISmsIrService smsIrService) {
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
//                FarazSmsCode => await farazSmsService.GetDashboardAsync(),
                SmsIrCode => await smsIrService.GetDashboardAsync(),
                _ => null
            };
        }

        public async Task<IReadOnlyList<SmsReportModel>> GetReportsAsync(int page = 1, int pageSize = 100)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting?.SmsServiceCode.ToString() switch {
//                FarazSmsCode => await farazSmsService.GetReportsAsync(page, pageSize),
                SmsIrCode => await smsIrService.GetReportsAsync(page, pageSize),
                _ => []
            };
        }
        public async Task<IReadOnlyList<SmsReceiveModel>> GetReceivesAsync(int page = 1, int pageSize = 100)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting?.SmsServiceCode.ToString() switch {
//                FarazSmsCode => await farazSmsService.GetReceivesAsync(page, pageSize),
                SmsIrCode => await smsIrService.GetReceivesAsync(page, pageSize),
                _ => []
            };
        }
    }
}