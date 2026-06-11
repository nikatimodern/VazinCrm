using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.SmsServices;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Chat;

public class ChatReminderService(CRMContextDb db, UserContextDb udb, ISmsQueue smsQueue, ILogger<ChatReminderService> logger) {
    public async Task CheckUnreadChats()
    {
        try
        {
            var reminderLimit = DateTime.Now.AddMinutes(-30);
            var smsCooldown = DateTime.Now.AddMinutes(-5);

            var mainChats = await db.MainChats
                .Include(x => x.Chats)
                .ToListAsync();

            var sentUsers = new HashSet<string>();

            foreach (var mainChat in mainChats)
            {
                var oldestUnread = mainChat?.Chats
                    .Where(x => !x.Read)
                    .OrderBy(x => x.CreateDate)
                    .FirstOrDefault();

                if (oldestUnread == null)
                    continue;

                if (oldestUnread.CreateDate > reminderLimit)
                    continue;

                var targetUserId = oldestUnread.UserCreatorID == mainChat.FromUserId ? mainChat.ToUserId : mainChat.FromUserId;

                if (mainChat.ReminderSentUserId == targetUserId)
                    continue;

                if (sentUsers.Contains(targetUserId))
                    continue;

                var phone = await udb.Users
                    .Where(x => x.Id == targetUserId)
                    .Select(x => x.PhoneNumber)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(phone))
                    continue;

                if (!TextService.IsValidPhoneNumber(phone))
                    continue;


                await smsQueue.QueueAsync(new SmsJob {
                    Type = SmsType.ChatReminder,
                    Mobile = phone,
                });

                mainChat.ReminderSentUserId = targetUserId;

                mainChat.ReminderSentDate = DateTime.Now;

                sentUsers.Add(targetUserId);
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat Reminder Error");
        }
    }
}