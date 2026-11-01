using System;

namespace CRM.Core.Domain.Deals;

public static class OverdueDealRules
{
    public static readonly DateTime StartDate = new(2026, 8, 27);
    public const int OverdueAfterDays = 14;
}
