using CRM.Core.Domain.Customers.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.PreFactors.Enum;

namespace CRM.Core.Domain.PreFactors.Entities {
    public class PreFactor : BaseEntity {
        public decimal SumTotalAmount { get; set; } //مبلغ کل
        public decimal SumTotalAmountTax { get; set; } // مبلغ کل به همراه مالیات منهای تخفیف
        public decimal Discount { get; set; } //تخفیف
        public int PercentTax { get; set; } //درصد مالیات
        public decimal TotalTax { get; set; } //مبلغ مالیات
        public decimal ReciveAmount { get; set; } // مبلغ دریافتی 
        public decimal PaymentAmount { get; set; } //باقیمانده
        public string Date { get; set; }
        public DateTime? DateMiladi { get; set; }
        public string ValidityDade { get; set; }
        public DateTime? ValidityDateMiladi { get; set; }
        public int SerialNumber { get; set; }

        public int CountNum { get; set; } = 0;

        public byte OfficialType { get; set; }
        public IList<PreFactorItem> PreFactorItems { get; set; }

        public Customer CustomerEntity { get; set; }
        public Guid? CustomerId { get; set; }
        public int? CustomerCode { get; set; }

        public string Customer { get; set; }
        public string AgentName { get; set; }
        public string AgentMeliCode { get; set; }
        public string Cus_EghtesadiCode { get; set; }
        public string Cus_NumberRegister { get; set; }
        public string Cus_ShenaseMeli { get; set; }
        public string Cus_PostalCode { get; set; }
        public string Cus_Mobile { get; set; }
        public string Cus_Tel { get; set; }
        public string Cus_Address { get; set; }
        public byte Cus_Juridical { get; set; }


        //public string CartableRolID { get; set; }
        //public string CartableRolTxt { get; set; }
        //public string CartableUserId { get; set; }
        //public string CartableUserTxt { get; set; }
        //public int CartableStep { get; set; }


        public IList<PreFactorDescription> Descriptions { get; set; }
        public IList<CRM.Core.Domain.Factors.Entities.Factor> Factors { get; set; }
        public string ExpireValidateDesc { get; set; }
        //public Customer Customer { get; set; }

        public int? PrePftype { get; set; }
        public int Pftype { get; set; } = 1;
        public string PftypeTxt { get; set; } = "در_دست_تایید";
        public TaminStepEnum TaminStep { get; set; }
        public string TaminStepTxt { get; set; }
        public FinancialStepEnum? FinancialStep { get; set; } = null;
        public DateTime? PftypeChangeDate { get; set; }
        public string PftypeChangeDateTxt { get; set; }
        public bool HasReturned { get; set; }
        public int? ReturnFromPftype { get; set; }

        public void RecordReturn()
        {
            if (!HasReturned)
                ReturnFromPftype = Pftype;

            HasReturned = true;
        }

        public void ClearReturn()
        {
            HasReturned = false;
            ReturnFromPftype = null;
        }

        public bool Accepted { get; set; } = false;
        public DateTime? AcceptedDate { get; set; }
        public string AcceptedDateTxt { get; set; }
        public string Emza { get; set; }
        public byte[] Signature { get; set; }
        public string SignatureDes { get; set; }
        public bool? Export { get; set; }
        public bool? AcceptedTerms { get; set; } // شرایط تکمیلی پیش فاکتور

        public Guid? UserTaminId { get; set; }
        public string UserTaminTxt { get; set; }


        public Guid? DealId { get; set; }
        public int? DealSerialNumber { get; set; }
        public Deal Deal { get; set; }

        public bool? AcceptedFinancial { get; set; } = null;
        public bool? AcceptedManagingDirector { get; set; } = null;


        public CurrencyTypeEnum CurrencyType { get; set; }
        public string CurrencyTypeTxt { get; set; }


        [NotMapped] public int RolCode { get; set; }
        [NotMapped] public int CountFactor { get; set; }
        [NotMapped] public int? CountFactorSepidar { get; set; }
        [NotMapped] public decimal? TotalSupplyAmount { get; set; }

        [NotMapped] public bool SupplyMismatch { get; set; }
        [NotMapped] public bool NoSupply { get; set; }
    }
}
