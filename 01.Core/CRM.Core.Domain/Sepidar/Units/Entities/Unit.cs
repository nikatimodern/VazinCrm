using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Units.Entities;


[Table("Unit", Schema = "INV")]
public class Unit {
    [Key]
    public int UnitID { get; set; }
    [MaxLength(50)]
    public string Title { get; set; }
    [MaxLength(50)]
    public string Title_En { get; set; }
    public int Version { get; set; }
}