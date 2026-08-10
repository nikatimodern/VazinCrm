using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Email;

public class EmailVModel {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string FromName { get; set; }
    public string ToName { get; set; }
    public string CreateDateTxt { get; set; }
    public bool Seen { get; set; }
}
