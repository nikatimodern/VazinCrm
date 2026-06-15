using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.SmsServices;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Chat;

public class ChatReminderService(CRMContextDb db, UserContextDb udb, ISmsQueue smsQueue, ILogger<ChatReminderService> logger) {
    public async Task CheckUnreadChats(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.Now;
            var initialDelayLimit = now.AddMinutes(-30); // قانون 30 دقیقه
            var maxAgeLimit = now.AddHours(-24); // قانون 24 ساعت
            var spamCooldownLimit = now.AddHours(-6); // قانون 6 ساعت استراحت

            // فقط چت‌هایی را می‌گیریم که پیام نخوانده‌ای دارند که عمرش بین 30 دقیقه تا 24 ساعت است
            var pendingMainChats = await db.MainChats
                .Include(m => m.Chats.Where(c => !c.Read && c.CreateDate >= maxAgeLimit))
                .Where(m => m.FromUserId != m.ToUserId)
                .Where(m => m.Chats.Any(c => !c.Read && c.CreateDate <= initialDelayLimit && c.CreateDate >= maxAgeLimit))
                .ToListAsync(cancellationToken);

            if (pendingMainChats.Count == 0) return;

            var jobsToQueue = new List<SmsJob>();
            var userIdsToFetch = new HashSet<string>();

            foreach (var mainChat in pendingMainChats)
            {
                var unreadChats = mainChat.Chats.OrderBy(x => x.CreateDate).ToList();

                var oldestUnread = unreadChats.FirstOrDefault();
                var latestUnread = unreadChats.LastOrDefault();

                if (oldestUnread == null) continue;

                var targetUserId = oldestUnread.UserCreatorID == mainChat.FromUserId ? mainChat.ToUserId : mainChat.FromUserId;


                var shouldSendSms = false;

                // شرط اول: آیا اصلا تا حالا پیامکی برای این چت رفته؟
                // یا اینکه پیامکی که قبلا رفته، مال قبل از این پیام‌های جدید بوده؟ (کاربر قبلی‌ها را خوانده و اینا کاملاً جدیدن)
                if (!mainChat.ReminderSentDate.HasValue || oldestUnread.CreateDate > mainChat.ReminderSentDate)
                {
                    shouldSendSms = true;
                } else
                {
                    // شرط دوم: کاربر پیام‌های قبلی را "نخوانده" است.
                    // آیا بعد از آخرین پیامک ما، پیام جدیدی برایش آمده؟
                    var hasNewMessagesSinceLastSms = latestUnread.CreateDate > mainChat.ReminderSentDate;

                    // آیا از آخرین پیامکی که فرستادیم، 6 ساعت (تایم استراحت) گذشته است؟
                    var cooldownPassed = mainChat.ReminderSentDate.Value <= spamCooldownLimit;

                    // اگر پیام جدیدی آمده بود و 6 ساعت هم گذشته بود، دوباره پیامک بده
                    if (hasNewMessagesSinceLastSms && cooldownPassed)
                    {
                        shouldSendSms = true;
                    }
                }

                if (!shouldSendSms) continue;

                userIdsToFetch.Add(targetUserId);

                mainChat.ReminderSentUserId = targetUserId;
                mainChat.ReminderSentDate = DateTime.Now;
            }

            if (userIdsToFetch.Count == 0) return;

            var usersPhones = await udb.Users
                .Where(x => userIdsToFetch.Contains(x.Id))
                .Select(x => new { x.Id, x.PhoneNumber })
                .ToDictionaryAsync(x => x.Id, x => x.PhoneNumber, cancellationToken);


            foreach (var mainChat in pendingMainChats)
            {
                var targetId = mainChat.ReminderSentUserId;
                if (string.IsNullOrEmpty(targetId)) continue;

                if (usersPhones.TryGetValue(targetId, out var phone) && TextService.IsValidPhoneNumber(phone))
                {
                    if (jobsToQueue.Any(j => j.Mobile == phone)) continue;

                    jobsToQueue.Add(new SmsJob {
                        Type = SmsType.ChatReminder,
                        Mobile = phone,
                    });
                }
            }

            foreach (var job in jobsToQueue)
            {
                await smsQueue.QueueAsync(job);
            }

            if (jobsToQueue.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("{Count} Chat reminders sent successfully.", jobsToQueue.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat Reminder Error");
        }
    }
}