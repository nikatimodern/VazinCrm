#nullable enable
using System;

namespace CRM.Core.Domain.Querying;
public sealed record QueryFilterInput(string? Search, int Type, int Count, string? EntityId, bool HasFilter, bool FilterCustomer, Guid? CustomerId, bool FilterDate, string? PersianFrom, string? PersianTo, bool FilterUser, Guid? UserId);
public sealed record QueryFilter(string? Search, int Type, int Count, string? EntityId, bool HasFilter, bool FilterCustomer, Guid? CustomerId, bool FilterDate, DateTime? From, DateTime? To, bool FilterUser, string? UserId);
