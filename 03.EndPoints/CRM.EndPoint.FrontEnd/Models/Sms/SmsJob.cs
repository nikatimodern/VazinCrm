using System;
using CRM.Core.Domain.Sms.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

public class SmsJob {
    public SmsPatternType PatternType { get; set; }
    public string Mobile { get; set; }
    public string SerialNumber { get; set; }
    public Guid? PfactorId { get; set; }
    public string Code { get; set; }
    public string ConnectionId { get; set; }
}

