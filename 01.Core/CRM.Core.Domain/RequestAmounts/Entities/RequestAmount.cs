using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable InconsistentNaming

namespace CRM.Core.Domain.RequestAmounts.Entities {
    public class RequestAmount : BaseEntity {
        public int RequestNumber { get; set; }
        public decimal Amount { get; set; }
        public RequestAmountDocType DocType { get; set; }
        public string DocTypeTxt { get; set; }
        public RequestStatusEnum RequestStatus { get; set; } = RequestStatusEnum.در_انتظار_تایید;
        public string RequestStatusTxt { get; set; }
        public decimal AcceptedAmount { get; set; }

        public IList<RequestAmountSupplyFactor> RequestAmountSupplyFactor { get; set; }
    }

    public enum RequestAmountDocType {
        نقدی = 1,
        چکی = 2
    }

    public enum RequestStatusEnum {
        در_انتظار_تایید = 1,
        تایید_شده = 2
    }
}