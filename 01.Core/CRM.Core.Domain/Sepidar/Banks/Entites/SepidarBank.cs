using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CRM.Core.Domain.Sepidar.Banks.Entites;

public class SepidarBank {
    [Key] 
    public int BankId { get; set; }
    public string Title { get; set; }
    public IList<SepidarBankBranch> SepidarBankBranches { get; set; }
}