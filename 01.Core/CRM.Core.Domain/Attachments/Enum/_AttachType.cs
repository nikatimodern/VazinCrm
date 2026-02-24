using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Attachments.Enum {
    public enum _AttachType {
        DocPayFactor = 1, // سند پرداختی برای فاکتور
        AcceptedCusPreFactor = 2, // سند تایید دستی پیشفاکتور
        ContractAttach = 3, //سند قرارداد
        DocPayPreFactor = 4, //سند پرداختی برای پیش فاکتور
        UploadCustomerPf = 5, //  تصاویر بارگزاری شده از طرف مشتری برای واریزی پیش فاکتور
        UploadSalesExpertPf = 6, //  تصاویر بارگزاری شده از طرف کارشناس برای واریزی پیش فاکتور
        Chat = 7, //  تصاویر بارگزاری شده پیام رسان
        SupplyFactor = 8, // تصویر بارگزاری شده فاکتور تامین
        Payment = 9, // تصویر بارگزاری شده پرداختی به تامین کننده
        Cash = 10 // رسید بارگزاری شده تنخواه
    }
}