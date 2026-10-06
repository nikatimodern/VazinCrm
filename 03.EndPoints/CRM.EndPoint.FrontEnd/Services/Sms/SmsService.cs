using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Entities;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Sms {
    public class SmsService(
        CRMContextDb db,
        UserContextDb udb,
        FarazSmsService farazSmsService,
        ISmsIrService smsIrService,
        DateTimeServices dtService) {
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
                return new ResponseSmsModel { Code = 0, Message = "سرویس پیامک تنظیم نشده است" };
            }

            try
            {
                return setting.SmsServiceCode.ToString() switch {
                    FarazSmsCode => await farazAction(),
                    SmsIrCode => await smsIrAction(),
                    _ => new ResponseSmsModel { Code = 0, Message = "سرویس پیامک نامعتبر است" }
                };
            }
            catch (Exception ex)
            {
                return new ResponseSmsModel { Code = 0, Message = ex.Message };
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


        public async Task SyncReceiveSmsAsync(CancellationToken cancellationToken = default)
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

            var contacts = await GetContactNamesAsync(
                items.Select(x => (x.Mobile, (SmsPatternType?)null)),
                cancellationToken);

            var farazIds = items
                .Select(x => x.MessageId)
                .Distinct()
                .ToList();

            var existingSms = await db.ReceivedSmses
                .Where(x => farazIds.Contains(x.FarazId))
                .ToDictionaryAsync(x => x.FarazId, cancellationToken);

            foreach (var item in items)
            {
                if (existingSms.TryGetValue(item.MessageId, out var existing))
                {
                    existing.Seen = item.Seen;
                    continue;
                }

                db.ReceivedSmses.Add(new ReceivedSms {
                    FarazId = item.MessageId,
                    Mobile = item.Mobile,
                    SenderName = contacts.GetValueOrDefault(item.Mobile),
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


        public async Task SyncSentSmsAsync(CancellationToken cancellationToken = default)
        {
            const int pageSize = 100;
            const int overlapHours = 1;

            var lastCreatedAt = await db.SentSmses
                .AsNoTracking()
                .Select(x => x.CreatedAtFaraz)
                .MaxAsync(cancellationToken);

            var syncFromDate = lastCreatedAt?.AddHours(-overlapHours);

            var page = 1;
            var items = new List<SmsReportModel>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await GetReportsAsync(page, pageSize);

                if (result.Data.Count == 0)
                    break;

                items.AddRange(
                    syncFromDate == null
                        ? result.Data
                        : result.Data.Where(x => (x.CreatedAtFaraz ?? x.SendDateTime) >= syncFromDate.Value)
                );

                var oldestItemInPage = result.Data.Min(x => x.CreatedAtFaraz ?? x.SendDateTime);

                if (syncFromDate != null && oldestItemInPage < syncFromDate.Value)
                    break;

                if (page >= result.TotalPages)
                    break;

                page++;

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            if (items.Count == 0)
                return;

            var contacts = await GetContactNamesAsync(items.Select(x => (x.Mobile, x.PatternType)), cancellationToken);

            var farazIds = items
                .Select(x => x.MessageId)
                .Distinct()
                .ToList();

            var existingSms = await db.SentSmses
                .Where(x => farazIds.Contains(x.FarazId))
                .ToDictionaryAsync(x => x.FarazId, cancellationToken);

            foreach (var item in items)
            {
                if (existingSms.TryGetValue(item.MessageId, out var existing))
                {
                    existing.DeliveryState = item.DeliveryState;
                    existing.DeliveryStateTxt = item.DeliveryStateTxt;
                    continue;
                }

                db.SentSmses.Add(new SentSms {
                    FarazId = item.MessageId,
                    Mobile = item.Mobile,
                    Number = item.Number,
                    ReceivedFullName = contacts.GetValueOrDefault(item.Mobile),

                    Type = item.Type,
                    TypeTxt = item.TypeTxt,

                    DeliveryState = item.DeliveryState,
                    DeliveryStateTxt = item.DeliveryStateTxt,

                    PatternType = item.PatternType,
                    PatternTypeTxt = item.PatternTypeTxt,

                    CreatedAtFaraz = item.CreatedAtFaraz,

                    SendDateTime = item.SendDateTime,
                    SendDateTimeTxt = item.SendDateTimeTxt,

                    MessageText = item.MessageText
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Dictionary<string, string>> GetContactNamesAsync(IEnumerable<(string Mobile, SmsPatternType? PatternType)> items,
            CancellationToken ct = default)
        {
            var data = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Mobile))
                .Select(x => (Mobile: TextService.NormalizeIranMobile(x.Mobile), x.PatternType))
                .Where(x => !string.IsNullOrWhiteSpace(x.Mobile))
                .ToList();

            var numbers = data.Select(x => x.Mobile).Distinct().ToList();

            var customerNumbers = data
                .Where(x => x.PatternType == null || IsCustomerPattern(x.PatternType))
                .Select(x => x.Mobile)
                .Distinct()
                .ToList();

            var contacts = new Dictionary<string, string>();

            var customers = await db.Customers
                .AsNoTracking()
                .Where(x => x.Mobile != null)
                .Select(x => new {
                    x.Mobile,
                    Name = $"{x.CompanyName ?? ""} {x.AgentName ?? ""}".Trim()
                })
                .ToListAsync(ct);

            foreach (var customer in customers)
            {
                var mobile = TextService.NormalizeIranMobile(customer.Mobile);

                if (customerNumbers.Contains(mobile) && !string.IsNullOrWhiteSpace(customer.Name))
                    contacts.TryAdd(mobile, customer.Name);
            }

            var missingNumbers = numbers.Except(contacts.Keys).ToList();

            var users = await udb.Users
                .AsNoTracking()
                .Where(x => missingNumbers.Contains(x.PhoneNumber) && !string.IsNullOrWhiteSpace(x.FullName) && x.FullName != x.PhoneNumber)
                .Select(x => new { x.PhoneNumber, x.FullName })
                .ToListAsync(ct);

            foreach (var user in users)
            {
                var mobile = TextService.NormalizeIranMobile(user.PhoneNumber);

                if (missingNumbers.Contains(mobile))
                    contacts.TryAdd(mobile, user.FullName);
            }

            return contacts;
        }

        private static bool IsCustomerPattern(SmsPatternType? patternType) =>
            patternType is
                SmsPatternType.AcceptPfactorToCustomer or
                SmsPatternType.AcceptedSalesAdminToCustomer or
                SmsPatternType.ReadyToDeliverToCustomer or
                SmsPatternType.PaymentLinkToCustomer or
                SmsPatternType.CustomerFeedback;

        public static SmsPatternType? GetPatternType(string patternCode, SmsPatterns patterns)
        {
            if (string.IsNullOrWhiteSpace(patternCode) || patterns == null)
                return null;

            foreach (var property in typeof(SmsPatterns).GetProperties())
            {
                var code = property.GetValue(patterns)?.ToString();

                if (code == patternCode && Enum.TryParse<SmsPatternType>(property.Name, out var type))
                {
                    return type;
                }
            }

            return null;
        }
    }
}