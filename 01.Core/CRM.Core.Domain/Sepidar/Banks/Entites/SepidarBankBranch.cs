using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CRM.Core.Domain.Sepidar.Banks.Entites;

public class SepidarBankBranch {
    [Key]
    public int BankBranchId { get; set; }
    public int BankRef { get; set; }
    public SepidarBank SepidarBank { get; set; }
    
    public string Title { get; set; }
    public IList<SepidarBankAccount> SepidarBankAccounts { get; set; }
}