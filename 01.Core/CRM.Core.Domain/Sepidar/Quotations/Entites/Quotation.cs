using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Quotations.Entites {
    [Table("Quotation", Schema = "SLS")]
    public class Quotation {
        [Key] public int QuotationId { get; set; }
        public int CustomerPartyRef { get; set; }
        public string CustomerPartyName { get; set; }
        public int Number { get; set; }
        public decimal Price { get; set; }
        public decimal? Tax { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? NetPrice { get; set; }

        public int SaleTypeRef { get; set; }
        public int? PartyAddressRef { get; set; }


        public DateTime Date { get; set; }
        public DateTime ExpirationDate { get; set; }

        public int CurrencyRef { get; set; }
        public int DeliveryLocationRef { get; set; }

        public decimal PriceInBaseCurrency { get; set; }

        public decimal? Discount { get; set; }
        public decimal? DiscountInBaseCurrency { get; set; }

        public decimal? Addition { get; set; }
        public decimal? AdditionInBaseCurrency { get; set; }

        public decimal? TaxInBaseCurrency { get; set; }

        public decimal? Duty { get; set; }
        public decimal? DutyInBaseCurrency { get; set; }

        public decimal NetPriceInBaseCurrency { get; private set; }

        public decimal Rate { get; set; }

        public int Version { get; set; }
        public int Creator { get; set; }
        public DateTime CreationDate { get; set; }
        public int LastModifier { get; set; }
        public DateTime LastModificationDate { get; set; }

        public bool Closed { get; set; }

        public int FiscalYearRef { get; set; }

        public int? ReceiptRef { get; set; }
        public int? PaymentRef { get; set; }

        [MaxLength(255)] public string CustomerRealName { get; set; }

        [MaxLength(255)] public string CustomerRealName_En { get; set; }

        public Guid? Guid { get; set; }

        public decimal? AdditionFactor_VatEffective { get; set; }
        public decimal? AdditionFactorInBaseCurrency_VatEffective { get; set; }

        public decimal? AdditionFactor_VatIneffective { get; set; }
        public decimal? AdditionFactorInBaseCurrency_VatIneffective { get; set; }


        public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    }
}