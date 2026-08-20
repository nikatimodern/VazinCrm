using System;
using System.Collections.Generic;
using CRM.Core.Domain.Chats.Entities;
using ChatEntity = CRM.Core.Domain.Chats.Entities.Chat;

namespace CRM.EndPoint.FrontEnd.Models.Chat;
public class ShowChatVModel
{
    public MainChat MainChat { get; set; }
    public List<ChatGroupVModel> Groups { get; set; } = [];
    public string CurrentUserId { get; set; }
    public Guid? FirstUnreadChatId { get; set; }
}
public class ChatGroupVModel
{
    public string DateTitle { get; set; }
    public List<ChatEntity> Chats { get; set; } = [];
}

