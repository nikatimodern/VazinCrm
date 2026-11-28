using System;
using System.Globalization;
using System.Linq;

namespace CRM.EndPoint.FrontEnd.Models.HumanResources;

internal static class HrPersianDate
{
    public static bool TryParseOptional(string value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var normalized = new string(value.Trim().Select(c => c switch
        {
            >= '\u06F0' and <= '\u06F9' => (char)('0' + c - '\u06F0'),
            >= '\u0660' and <= '\u0669' => (char)('0' + c - '\u0660'),
            _ => c
        }).ToArray());
        var parts = normalized.Split('/');
        if (parts.Length != 3 || parts[0].Length != 4 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
            return false;
        try
        {
            date = DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0));
            return true;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}
