namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarPartyDto {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EconomicCode { get; set; }
    public string IdentificationCode { get; set; }
    public string DlCode { get; set; }
    public int DlRef { get; set; }
}