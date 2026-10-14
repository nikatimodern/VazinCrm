using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Sepidar.DLs.Entites;

namespace CRM.Core.Domain.Sepidar.Banks.Entites;

[Table("BankAccount", Schema = "RPA")]
public class SepidarBankAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int BankAccountId { get; set; }

    public int BankBranchRef { get; set; }
    public SepidarBankBranch SepidarBankBranch { get; set; }

    [Required]
    [MaxLength(250)]
    public string AccountNo { get; set; } = string.Empty;

    public int AccountTypeRef { get; set; }

    public int DlRef { get; set; }
    public DL Dl { get; set; }
    

    public int CurrencyRef { get; set; }

    [Column(TypeName = "decimal(26,16)")]
    public decimal Rate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal FirstAmount { get; set; }

    public DateTime FirstDate { get; set; }

    public int Version { get; set; }

    public int? Creator { get; set; }

    public DateTime? CreationDate { get; set; }

    public int? LastModifier { get; set; }

    public DateTime? LastModificationDate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Balance { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? BillFirstAmount { get; set; }

    [MaxLength(250)]
    public string? ClearFormatName { get; set; }

    [MaxLength(4000)]
    public string? Owner { get; set; }

    [MaxLength(4000)]
    public string? Owner_En { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? BlockedAmount { get; set; }

    public bool ShowBankFeeSeparately { get; set; }

    [MaxLength(250)]
    public string? CreditCardNumber { get; set; }

    [MaxLength(30)]
    public string? ShebaNumber { get; set; }
}