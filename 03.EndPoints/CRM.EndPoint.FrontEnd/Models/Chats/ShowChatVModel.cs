using System;
using System.Collections.Generic;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Chats.Entities;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace CRM.EndPoint.FrontEnd.Models.Chats;
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
    public List<Chat> Chats { get; set; } = [];
}

