using System.Collections.Generic;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Chat;

public class SendChatVModel {
    public Core.Domain.Chats.Entities.Chat Chat { get; set; }
    public IList<Attachment> Attachments { get; set; }
}
