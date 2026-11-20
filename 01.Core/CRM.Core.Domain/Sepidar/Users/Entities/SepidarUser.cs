using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Users.Entities;

// Read-only projection of FMK.User for account mapping. Never load passwords.
[Table("User", Schema = "FMK")]
public class SepidarUser
{
    [Key]
    public int UserID { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(200)]
    public string UserName { get; set; } = null!;

    public bool? Status { get; set; }
    public bool IsDeleted { get; set; }
}
