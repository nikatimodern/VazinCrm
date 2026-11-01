#nullable enable
using System;
namespace CRM.Core.Domain.Reports.Models;
public sealed record ReportSearch(
    string? Search = null, int Type = 0, int Count = 0,
    Guid? ResultCallId = null, Guid? FinalResultCallId = null, Guid? ReasonCancelId = null,
    bool FilterCustomer = false, Guid? CustomerId = null,
    bool FilterUser = false, string? UserId = null,
    bool FilterDate = false, DateTime? FromDate = null, DateTime? ToDate = null);
