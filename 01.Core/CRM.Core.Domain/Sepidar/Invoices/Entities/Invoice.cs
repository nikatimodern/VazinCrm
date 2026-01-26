using CRM.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CRM.Core.Domain.Sepidar.Invoices.Entities
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int? Number { get; set; }//شماره سریال
        public string CustomerPartyName { get; set; }// نام مشتری
        public int? CustomerPartyRef { get; set; }// کد مشتری
        public string CustomerEconomicCode { get; set; }//  کد اقتصادی
        public string CustomerIdentificationCode { get; set; }//  شماره ثبت /شماره ملی 
        public string CustomerState { get; set; }//  استان مشتری 
        public string CustomerZipCode { get; set; }//  کد پستی 
        public string CustomerCity { get; set; }//  شهر مشتری 
        public string PartyAddress { get; set; }// آدرس مشتری 
        public decimal? Price { get; set; }// مبلغ
        public decimal? Discount { get; set; } // تخفیف
        public decimal? Addition { get; set; } // اضافات
        public decimal? PriceInBaseCurrency { get; set; }// مبلغ بعد از تخفیف و اضافات
        public decimal? Tax { get; set; }// مالیات و ارزش افزوده
        public decimal? NetPrice { get; set; }// مبلغ کل

        public int? State { get; set; } // 2: ابطال شده

        public int? QuotationNumber { get; set; }
        public int? QuotationRef { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? CreationDate { get; set; }
        public string DateTXT { get { return BaseServices.GetDatePersian(Date); }}
        public IList<InvoiceItem> InvoiceItems { get; set;}

        [NotMapped]
        public string ExtraDescription { get; set; }


    }
}
