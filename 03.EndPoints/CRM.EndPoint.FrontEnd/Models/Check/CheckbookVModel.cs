using CRM.Core.Domain.Checks.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Check;

public class CheckbookVModel {
    public Checkbook Checkbook { get; set; }
    public int ChecksCount { get; set; }
}