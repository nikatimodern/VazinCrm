using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel
{

    public class filterVModel
    {
        public string EntityId { get; set; }
        public string serVal { get; set; }
        public int _type { get; set; }
        public int Count { get; set; }//تعداد نمایش
        public filterV? filt { get; set; }
    }
    
    public class AccfilterVModel : filterVModel
    {
        public int PayType { get; set; }
        public int AccType { get; set; }
    }

    public class filterV
    {
        public bool CusFilterChd { get; set; }
        public bool SelFilterChd { get; set; }
        public bool DateFilterChk { get; set; }

        public Guid? customerFilter { get; set; }
        public string customerFilterTxt { get; set; }

        public Guid? selFilter { get; set; }
        public string selFilterTxt { get; set; }

        public string fromDate { get; set; }
        public string toDate { get; set; }
    }

}
