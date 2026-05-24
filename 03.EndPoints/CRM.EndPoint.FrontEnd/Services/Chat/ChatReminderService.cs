using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.Factor;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Chat;

public class ChatReminderService {
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly SmsServices sms;
    private readonly ILogger<ChatReminderService> logger;

    public ChatReminderService(CRMContextDb db, UserContextDb udb, SmsServices sms, ILogger<ChatReminderService> logger)
    {
        this.db = db;
        this.udb = udb;
        this.sms = sms;
        this.logger = logger;
        this.sms = sms;
    }

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


                await sms.SendChatRemind(phone);

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