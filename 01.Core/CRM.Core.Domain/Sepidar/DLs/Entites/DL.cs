using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Sepidar.Banks.Entites;

namespace CRM.Core.Domain.Sepidar.DLs.Entites;

[Table("DL", Schema = "ACC")]
public class DL {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int DLId { get; set; }

    public SepidarBankAccount SepidarBankAccount { get; set; }

    [Required] [MaxLength(40)] 
    public string Code { get; set; } = string.Empty;

    [Required] [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required] [MaxLength(250)]
    public string Title_En { get; set; } = string.Empty;

    public int Type { get; set; }

    public int Version { get; set; }

    public int Creator { get; set; }

    public DateTime CreationDate { get; set; }

    public int LastModifier { get; set; }

    public DateTime LastModificationDate { get; set; }

    public bool IsActive { get; set; }
}