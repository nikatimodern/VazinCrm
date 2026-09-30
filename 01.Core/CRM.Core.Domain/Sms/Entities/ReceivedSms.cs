using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Sms.Entities;

public class ReceivedSms : BaseEntity {
    public long FarazId { get; set; }
    public string Mobile { get; set; }
    public string MessageText { get; set; }
    public string Number { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public string ReceivedDateTimeTxt { get; set; }
    public DateTime CreatedAtFaraz { get; set; }
    public bool Seen { get; set; }
}