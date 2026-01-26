using System.Collections.Generic;
using CRM.Core.Domain.AppUsers.Entities;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace CRM.EndPoint.FrontEnd.Models.Chats;

public class ChatVModel {
    public IList<AppUser> SalesExpert { get; set; }
    public string UserMobile { get; set; }
}
