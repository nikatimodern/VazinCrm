using System;

namespace CRM.EndPoint.FrontEnd.Models {
    public class FilterVModel {
        public string EntityId { get; set; }
        public string serVal { get; set; }
        public int _type { get; set; }
        public int Count { get; set; } //تعداد نمایش
        public FilterV filt { get; set; }
    }

    public class ReportFilterVModel : FilterVModel {
        public Guid? ResultCallId { get; set; }
        public Guid? FinalResultCallId { get; set; }
        public Guid? ReasonCancelId { get; set; }
    }

    public class AccFilterVModel : FilterVModel {
        public int PayType { get; set; }
        public int AccType { get; set; }
    }

    public class FilterV {
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