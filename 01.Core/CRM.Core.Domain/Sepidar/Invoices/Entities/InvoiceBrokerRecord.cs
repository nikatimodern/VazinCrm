using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Invoices.Entities;

// SLS.InvoiceBroker is not an IDENTITY table. BrokerID is allocated by FMK.spGetNextId.
[Table("InvoiceBroker", Schema = "SLS")]
public sealed class InvoiceBrokerRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int BrokerID { get; set; }
    public int InvoiceRef { get; set; }
    public int PartyRef { get; set; }
    public decimal Commission { get; set; }
    public decimal Rate { get; set; }
    public decimal CommissionInBaseCurrency { get; set; }
}
