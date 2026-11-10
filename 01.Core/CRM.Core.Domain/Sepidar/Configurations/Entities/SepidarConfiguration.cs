using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Configurations.Entities;

/// <summary>
/// Sepidar's own configuration table. Account and DL settings store internal
/// ACC.Account.AccountId and ACC.DL.DLId values (not their displayed codes).
/// </summary>
[Table("Configuration", Schema = "FMK")]
public sealed class SepidarConfiguration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ConfigurationID { get; set; }

    [Required]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public int Version { get; set; }
}
