using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Cashes.Entities;

[Table("Cash", Schema = "RPA")]
public class SepidarCash {
    [Key]
    public int CashId { get; set; }
    public int DlRef { get; set; }
    public string Title { get; set; }
}