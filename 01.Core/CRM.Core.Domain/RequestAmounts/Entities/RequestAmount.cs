using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.RequestAmounts.Entities
{
    public class RequestAmount
    {
        public string Amount { get; set; }
        public RequestAmountDocType? DocType { get; set; }
        public string DocTypeTXT { get; set; }

    }

    public enum RequestAmountDocType
    {
        چکی,
        نقدی
    }

}
