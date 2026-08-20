namespace CRM.EndPoint.FrontEnd.Models;

public class ErrorVModel {
    public int Code { get; set; }
    public string Message { get; set; }
    public string Title { get; set; }
}

public class ErrorModel
{
    public bool Status { get; set; }
    public object Result { get; set; }
}