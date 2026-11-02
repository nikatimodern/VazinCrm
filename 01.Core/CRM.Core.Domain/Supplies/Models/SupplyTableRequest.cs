#nullable enable
using System;

namespace CRM.Core.Domain.Supplies.Models;
public sealed record SupplyTableRequest(string? Draw, int Start, int Length, string? SortColumn, string? SortDirection, string? Search, int? Type, bool DateFilterEnabled, DateTime? FromDate, DateTime? ToDate, bool UserFilterEnabled, string? UserId, string? PersianFrom = null, string? PersianTo = null, bool RawDateInput = false, string? CustomerId = null);
public sealed record SupplySelection(string? Search, System.Collections.Generic.List<Guid> SelectedIds);
