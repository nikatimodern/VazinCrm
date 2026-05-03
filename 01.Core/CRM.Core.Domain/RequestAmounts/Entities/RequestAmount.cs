using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using CRM.Core.Domain.RequestAmounts.Enums;


namespace CRM.Core.Domain.RequestAmounts.Entities {
    public class RequestAmount : BaseEntity {
        public int RequestNumber { get; set; }
        public decimal Amount { get; set; }
        public RequestAmountDocTypeEnum DocTypeEnum { get; set; }
        public string DocTypeTxt { get; set; }
        public RequestStatusEnum RequestStatus { get; set; } = RequestStatusEnum.ثبت_اولیه;
        public string RequestStatusTxt { get; set; }
        public decimal AcceptedAmount { get; set; }
        public bool Vosoled { get; set; }

        public IList<RequestAmountSupplyFactor> RequestAmountSupplyFactor { get; set; }
        public IList<RequestAmountOrderHead> RequestAmountOrderHeads { get; set; }

        public Guid? SupplierId { get; set; }
        public string SupplierTxt { get; set; }

        public bool Prepayment { get; set; }
        public string Description { get; set; }
    }
}