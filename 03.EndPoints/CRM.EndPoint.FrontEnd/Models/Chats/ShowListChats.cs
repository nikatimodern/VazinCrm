using System;

namespace CRM.EndPoint.FrontEnd.Models.Chats;

public class ShowListChats {
    public string FullName { get; set; }
    public string PersonImageUrl { get; set; }
    public int CountNotRead { get; set; }
    public Guid MainChatId { get; set; }
}