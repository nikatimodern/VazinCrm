using System;

namespace CRM.EndPoint.FrontEnd.Models.Supply;

public class SuppliesDemandsDtVModel {
    public Guid DealId { get; set; }
    public Guid? CustomerId { get; set; }
    public string DealTitle { get; set; }



    //نام مشتری
    public string CustomerName { get; set; }
    public int? CustomerCode { get; set; }



    //شماره سفارش
    public int? DealSerialNumber { get; set; }

    //تاریخ سفارش
    public string DealDate { get; set; }
    public DateTime? DealDateM { get; set; }

    //جمع مبلغ فاکتور های سفارش
    public decimal TotalFactorsAmount { get; set; }

    //دریافت نقدی
    public decimal TotalAccAmount { get; set; }

    //مطالبات
    public decimal Demand { get; set; }


    public int PfSerialNumber { get; set; }
    public string UserCreatorDeal { get; set; }
    public string UserCreatorDealId { get; set; }
}