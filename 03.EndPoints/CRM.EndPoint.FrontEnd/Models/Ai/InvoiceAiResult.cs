using System;
using System.Collections.Generic;
using CRM.Core.Domain.Warehouses.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Ai;

public class InvoiceAiResult {
    public decimal SumTotalAmount { get; set; } // مبلغ کل قبل از تخفیف و مالیات
    public decimal Discount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal PaymentAmount { get; set; }

    public string Date { get; set; }
    public DateTime? DateMiladi { get; set; }

    public string SerialNumber { get; set; }

    public byte OfficialType { get; set; } // 1 رسمی - 0 غیررسمی

    public IList<InvoiceItem> FactorItems { get; set; } = new List<InvoiceItem>();
}

public class InvoiceItem {
    public string Product { get; set; }

    public decimal Weight { get; set; }

    public UnitTypeEnums Unit { get; set; }

    public string UnitTxt { get; set; }

    public decimal UnitAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxAmount { get; set; }

    public Guid? ProductCode { get; set; }

    public string ProductSmallName { get; set; }

    public byte? Printable { get; set; } = 0;
}