using System.Linq;

namespace CRM.Core.ApplicationServices.Accounts.Services;

public static class AccountOtpPhone
{
    public static string? Normalize(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return null;
        var value = new string(mobile.Trim().Select(c => c switch
        {
            >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
            >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
            _ => c
        }).ToArray()).Replace(" ", "").Replace("-", "");

        if (value.StartsWith("+98")) value = "0" + value[3..];
        else if (value.StartsWith("0098")) value = "0" + value[4..];
        else if (value.StartsWith("98")) value = "0" + value[2..];

        return value.Length == 11 && value.StartsWith("09") && value.All(c => c is >= '0' and <= '9')
            ? value
            : null;
    }
}
