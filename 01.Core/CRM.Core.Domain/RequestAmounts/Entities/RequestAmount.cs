using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;


namespace CRM.Core.Domain.RequestAmounts.Entities {
    public class RequestAmount : BaseEntity {
        public int RequestNumber { get; set; }
        public decimal Amount { get; set; }
        public RequestAmountDocTypeEnum DocType { get; set; }
        public string DocTypeTxt { get; set; }
        public RequestStatusEnum RequestStatus { get; set; } = RequestStatusEnum.ثبت_اولیه;
        public string RequestStatusTxt { get; set; }
        public decimal AcceptedAmount { get; set; }


        public int? AcceptedAmountPercent { get; set; }
        public IList<RequestAmountSupplyFactor> RequestAmountSupplyFactor { get; set; }
        public IList<RequestAmountOrderHead> RequestAmountOrderHeads { get; set; }

        public Guid? SupplierId { get; set; }
        public string SupplierTxt { get; set; }

        public Guid? CashierId { get; set; } // تنخواه دار
        public string CashierTxt { get; set; }

        public string Description { get; set; }

        public RequestAmountTypeEnum RequestAmountType { get; set; }
        public string RequestAmountTypeTxt { get; set; }

        public IList<RequestAmountPayment> RequestAmountPayments { get; set; }

        public Accountancy Accountancy { get; set; }
    }
}