using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Currencies.Entites;

[Table("Currency", Schema = "GNR")]
public class Currency {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int CurrencyID { get; set; }

    [Required] [MaxLength(40)] public string Title { get; set; } = string.Empty;

    [Required] [MaxLength(40)] public string Title_En { get; set; } = string.Empty;

    public int ExchangeUnit { get; set; }

    public int PrecisionCount { get; set; }

    [MaxLength(40)] public string PrecisionName { get; set; }

    [MaxLength(40)] public string PrecisionName_En { get; set; }

    public int Version { get; set; }
}