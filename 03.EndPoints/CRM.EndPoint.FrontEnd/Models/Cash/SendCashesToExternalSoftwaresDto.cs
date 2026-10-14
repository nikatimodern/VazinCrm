using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class SendCashesToExternalSoftwaresDto {
    public IList<Guid> CashIds { get; set; }
    public IList<Guid> ExternalSoftwareIds { get; set; }
}