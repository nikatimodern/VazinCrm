using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Chats.Entities;

public class Chat : BaseEntity
{
    public string Text { get; set; }
    public string FileAddress { get; set; }
    public bool Read { get; set; }
    public Guid MainChatId { get; set; }
}