using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models;

public class ReturnVModel {
    public bool Status { get; set; }
    public List<ErrorVModel> Errors { get; set; }
    public object Data { get; set; }
}