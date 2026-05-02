using CRM.Framework.Domain;
using System;
using System.Collections.Generic;


// ReSharper disable InconsistentNaming

namespace CRM.Core.Domain.RequestAmounts.Entities {
    public class RequestAmount : BaseEntity {
        public int RequestNumber { get; set; }
        public decimal Amount { get; set; }
        public RequestAmountDocType DocType { get; set; }
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

    public enum RequestAmountDocType {
        نقدی = 1,
        چکی = 2
    }

    public enum RequestStatusEnum {
        ثبت_اولیه = 0,
        در_انتظار_تایید_مدیرعامل = 1,
        در_انتظار_ارسال_به_مالی = 2,
        در_انتظار_پرداخت = 3,
        واریز_شد = 4,
    }
}