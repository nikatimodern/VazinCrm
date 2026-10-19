using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Parties;


[Table("Party", Schema = "GNR")]
public class Party {
    [Key]
    public int PartyId { get; set; }

    public int Type { get; set; }
    public int SubType { get; set; }

    [MaxLength(250)]
    public string Name { get; set; } = null!;

    [MaxLength(250)]
    public string LastName { get; set; } = null!;

    [MaxLength(250)]
    public string Name_En { get; set; } = null!;

    [MaxLength(250)]
    public string LastName_En { get; set; } = null!;

    [MaxLength(40)]
    public string EconomicCode { get; set; }

    [MaxLength(40)]
    public string IdentificationCode { get; set; }

    [MaxLength(40)]
    public string RegistrationCode { get; set; }

    [MaxLength(250)]
    public string Website { get; set; }

    [MaxLength(250)]
    public string Email { get; set; }

    public int DLRef { get; set; }

    public bool IsInBlacklist { get; set; }
    public bool IsVendor { get; set; }

    public int? VendorGroupingRef { get; set; }
    public bool IsBroker { get; set; }

    public decimal CommissionRate { get; set; }
    public decimal BrokerOpeningBalance { get; set; }
    public int BrokerOpeningBalanceType { get; set; }

    public bool IsEmployee { get; set; }
    public bool IsCustomer  { get; set; }

    public int? SalespersonPartyRef { get; set; }

    public decimal DiscountRate { get; set; }
    public decimal MaximumCredit { get; set; }

    public int? CustomerGroupingRef { get; set; }
    public int CustomerCategoryForTax { get; set; }

    public DateTime? BirthDate { get; set; }
    public DateTime? MarriageDate { get; set; }

    public bool HasCredit { get; set; }
    public int CreditCheckingType { get; set; }

    public int Version { get; set; }
    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }

    public decimal CustomerRemaining { get; set; }
    public int BrokerGroupingRef { get; set; }

    [MaxLength(36)]
    public string Guid { get; set; }

    [MaxLength(9)]
    public string PassportNumber { get; set; }

    public bool IsPurchasingAgent { get; set; }

    public int MaximumQuantityCredit { get; set; }
    public bool HasQuantityCredit { get; set; }
    public int QuantityCreditCheckingType { get; set; }
}