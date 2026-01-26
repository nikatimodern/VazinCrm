using CRM.Core.Domain.LogSystems.Entities;

namespace CRM.EndPoint.FrontEnd.Models.LogSystem;

public class LogSystemModel<T> {
    public T OldEntity { get; set; }
    public T NewEntity { get; set; }
    public int? SerialNumber { get; set; }
    public LogActionEnum Action { get; set; }
    public string FullName { get; set; }
    public string CommentTxt { get; set; }
}
