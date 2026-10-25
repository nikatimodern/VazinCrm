using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public class SepidarStockDto {
    public int Id { get; set; }
    public int Code { get; set; }
    public string Title { get; set; }
}

public class SepidarStockItemDto {
    public List<SepidarStockDto> Stocks { get; set; }
    public int ItemRef { get; set; }
}