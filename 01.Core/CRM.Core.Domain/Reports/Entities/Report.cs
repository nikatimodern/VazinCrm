using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.Core.Domain.Reports.Enum;

namespace CRM.Core.Domain.Reports.Entities {
    public class Report : BaseEntity {
        public ReportTypeEnum ReportType { get; set; }
        public string ReportTypeTxt { get; set; }
        public string RepTel { get; set; }
        public Guid? CustomerId { get; set; }
        public int? CustomerCode { get; set; }
        public string CustomerTxt { get; set; }
        public string ReportText { get; set; }
        public DateTime RegDate { get; set; }
        public string RegDateTxt { get; set; }
        public string RegTimeTxt { get; set; }

        public Guid? DealId { get; set; }
        public int? DealSerialNumber { get; set; }

        public Guid? ResultCallId { get; set; }
        public string ResultCallTxt { get; set; }
        public Guid? FinalResultCallId { get; set; }
        public string FinalResultCallTxt { get; set; }
        public Guid? ReasonCancelId { get; set; }
        public string ReasonCancelTxt { get; set; }

        public bool AI { get; set; }
        public Guid? CallRecordingId { get; set; }
        public CallRecording CallRecording { get; set; }
        public IList<ReportParticipant> Participants { get; set; }
    }
}

