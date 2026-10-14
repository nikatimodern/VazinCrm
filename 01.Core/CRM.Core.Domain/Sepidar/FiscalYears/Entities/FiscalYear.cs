using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.FiscalYears.Entities;

[Table("FiscalYear", Schema = "FMK")]
public class FiscalYear
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int FiscalYearId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Title_En { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Status { get; set; }

    public int Creator { get; set; }

    public DateTime CreationDate { get; set; }

    public int LastModifier { get; set; }

    public DateTime LastModificationDate { get; set; }

    public int Version { get; set; }
}