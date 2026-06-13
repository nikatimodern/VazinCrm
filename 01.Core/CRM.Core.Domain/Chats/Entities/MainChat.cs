using System;
using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Chats.Entities;

public class MainChat : BaseEntity {
    public string FromUserTxt { get; set; }
    public string FromUserId { get; set; }
    public string ToUserTxt { get; set; }
    public string ToUserId { get; set; }
    public List<Chat> Chats { get; set; }

    public string ReminderSentUserId  { get; set; }
    public DateTime? ReminderSentDate { get; set; }
}