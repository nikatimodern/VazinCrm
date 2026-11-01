using System;
using System.Collections.Generic;
namespace CRM.Core.Domain.Reports.Models;
public sealed record ReportLookup(Guid Value, string Text);
public sealed record ReportBaseItems(List<ReportLookup> ResultCall, List<ReportLookup> FinalResultCall, List<ReportLookup> ReasonCancel);
