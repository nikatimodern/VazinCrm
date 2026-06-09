using System;
using CRM.Core.Domain.Checks.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Checks.Entities;

public class Check : BaseEntity
{
    public Guid CheckbookId { get; set; }
    public Checkbook Checkbook { get; set; }

    public string SerialNumber { get; set; }

    public decimal Amount { get; set; }
    public DateTime? IssueDate { get; set; }   // تاریخ صدور
    public DateTime? DueDate { get; set; }     // تاریخ سررسید چک
    public string receiver { get; set; }         // در وجه (گیرنده چک)
    public string Description { get; set; }   // توضیحات


    public CheckStatus Status { get; set; } = CheckStatus.سفید; // وضعیت فعلی چک
}