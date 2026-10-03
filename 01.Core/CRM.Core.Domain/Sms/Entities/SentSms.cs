using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Sms.Entities;

public class SentSms : BaseEntity {
    public long FarazId { get; set; }

    public string Mobile { get; set; }
    public string Number { get; set; }

    public string Type { get; set; }
    public string TypeTxt { get; set; }

    public string DeliveryState { get; set; }
    public string DeliveryStateTxt { get; set; }

    public DateTime? CreatedAtFaraz { get; set; }

    public DateTime SendDateTime { get; set; }
    public string SendDateTimeTxt { get; set; }

    public string MessageText { get; set; }
}