using System;

namespace CRM.Core.Domain.Supplies.Models;

public static class SupplyPurchaseUnit
{
    public static bool Matches(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) &&
        string.Equals(Normalize(first), Normalize(second), StringComparison.Ordinal);

    private static string Normalize(string unit)
    {
        unit = unit.Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک').Replace(" ", "").Replace("\u200c", "");
        return unit switch
        {
            "رولی" => "رول",
            "مترطول" => "متر",
            _ => unit
        };
    }
}
