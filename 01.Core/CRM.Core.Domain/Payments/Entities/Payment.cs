using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.RequestAmounts.Entities;

namespace CRM.Core.Domain.Payments.Entities {
    public class Payment : BaseEntity {
        public int PaymentNumber { get; set; }
        public decimal Amount { get; set; }
        public string FromAccountTxt { get; set; }
        public Guid FromAccountId { get; set; }
        public string ToAccountTxt { get; set; }
        public Guid ToAccountId { get; set; }


        public PayTypeEnum PaymentType { get; set; }
        public string PaymentTypeTxt { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentDateTxt { get; set; }

        [NotMapped] public IList<Attachment> Attachments { get; set; }

        public IList<PaymentSupplyFactor> PaymentSupplyFactors { get; set; }

        public IList<RequestAmountPayment> RequestAmountPayments { get; set; }

        private PaymentModeEnum _paymentMode;
        public PaymentModeEnum PaymentMode {
            get => _paymentMode;
            set {
                _paymentMode = value;
                PaymentModeTxt = value.ToString().Replace("_"," ");
            }
        }
        public string PaymentModeTxt { get; private set; }

    }
}