namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarItemDto {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BarCode { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DutyRate { get; set; }
    public int? DefaultStockRef { get; set; }
}