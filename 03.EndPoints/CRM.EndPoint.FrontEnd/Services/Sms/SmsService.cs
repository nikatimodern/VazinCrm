using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services.Sms {
    public class SmsService(CRMContextDb db, FarazSmsService farazSmsService, ISmsIrService smsIrService) {
        private const string FarazSmsCode = "cdde93d8-0522-4370-a734-008265e10245";
        private const string SmsIrCode = "c02f8771-cf60-4891-84cb-a21de966b3fc";

        public async Task<ResponseSmsModel> SendAsync(SmsJob job)
        {
            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            return setting.SmsServiceCode.ToString() switch {
                FarazSmsCode => await farazSmsService.SendPatternSms(job),
                SmsIrCode => await smsIrService.SendPatternSms(job),
                _ => null
            };
        }
    }
}