using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Parties;


[Table("PartyAddress", Schema = "GNR")]
public class PartyAddress {
    [Key]
    public int PartyAddressId { get; set; }
    public int PartyRef { get; set; }
    public int IsMain { get; set; }
    public int Type { get; set; }
    
    [MaxLength(400)]
    public int Address { get; set; }

}