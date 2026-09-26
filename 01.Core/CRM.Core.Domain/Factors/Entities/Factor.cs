using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Factors.Entities {
    public class Factor : BaseEntity {
        
        public decimal SumTotalAmount { get; set; }
        public decimal SumTotalAmountTax { get; set; }
        public decimal Discount { get; set; }
        public int PercentTax { get; set; } //درصد مالیات
        public decimal TotalTax { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal ReciveAmount { get; set; }
        public string Date { get; set; }
        public DateTime? DateMiladi { get; set; }
        public DateTime? DateCompleted { get; set; }
        public string DateCompletedTxt { get; set; }
        public string SnFinancial { get; set; }
        public int? SepidarId { get; set; } // InvoiceId کلید اصلی فاکتور سپیدار
        public int SerialNumber { get; set; }
        public int CountNum { get; set; } = 0;
        public byte OfficialType { get; set; }


        public Guid? DealId { get; set; }
        public int? DealSerialNumber { get; set; }
        public Deal Deal { get; set; }

        public Guid? CustomerId { get; set; }
        public string Customer { get; set; }
        public int? CustomerCode { get; set; }


        public string AgentName { get; set; }
        public string AgentMeliCode { get; set; }
        public string Cus_EghtesadiCode { get; set; }
        public string Cus_NumberRegister { get; set; }
        public string Cus_ShenaseMeli { get; set; }
        public string Cus_PostalCode { get; set; }
        public string Cus_Mobile { get; set; }
        public string Cus_Tel { get; set; }
        public string Cus_Address { get; set; }


        public string ExpireValidateDesc { get; set; }
        //public Customer Customer { get; set; }

        public int ftype { get; set; }
        public string ftypeTxt { get; set; }


        public string Emza { get; set; }
        public byte[] Signature { get; set; }
        public string SignatureDes { get; set; }

        public string SelerFullName { get; set; } = string.Empty;
        public string SelerId { get; set; }


        public int PfactorSerialNumber { get; set; }
        public Guid PfactorID { get; set; }


        public IList<FactorItem> FactorItems { get; set; }
        public IList<FactorDescription> Descriptions { get; set; }

        public Guid? UserTaminId { get; set; }
        public string UserTaminTxt { get; set; }

        public bool? Export { get; set; }

        [NotMapped] public bool Financial { get; set; } = false;

        //public bool? Accepted { get; set; } = false;

        public FactorStatus status { get; set; } = FactorStatus.در_دست_تایید;
        public string statusTxt { get; set; } = nameof(FactorStatus.در_دست_تایید).Replace("_", " ");


        public CurrencyTypeEnum CurrencyType { get; set; }
        public string CurrencyTypeTxt { get; set; }

        public bool Returned { get; set; }
        public Guid? ParentReturnedId { get; set; }
        public DateTime? CustomerFeedbackSmsSentDate { get; set; }
    }
}