using System;

namespace CRM.Core.Domain.Sepidar.Accounts.Entites;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Account", Schema = "ACC")]
public class Account
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int AccountId { get; set; }

    public int? ParentAccountRef { get; set; }

    public int Type { get; set; }

    [Required]
    [MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Title_En { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int? CashFlowCategory { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? OpeningBalance { get; set; }

    public int BalanceType { get; set; }

    public bool HasBalanceTypeCheck { get; set; }

    public bool HasDL { get; set; }

    public bool HasCurrency { get; set; }

    public bool HasCurrencyConversion { get; set; }

    public bool HasTracking { get; set; }

    public bool HasTrackingCheck { get; set; }

    public int Version { get; set; }

    public int Creator { get; set; }

    public DateTime CreationDate { get; set; }

    public int LastModifier { get; set; }

    public DateTime LastModificationDate { get; set; }

    public bool ShowInCompanyDashboard { get; set; }

    public bool ShowInAccountAnalysisReport { get; set; }
}