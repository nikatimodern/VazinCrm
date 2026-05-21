using System;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Chats.Entities;

public class Chat : BaseEntity {
    public string Text { get; set; }
    public string FileAddress { get; set; }
    public bool Read { get; set; }
    public Guid MainChatId { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? EditDate { get; set; }
    public string EditDateTxt { get; set; }

    [NotMapped] public string FileName { get; set; }

    [NotMapped] public string FileSize { get; set; }
}