namespace CRM.Core.ApplicationServices.HumanResources.Services;

public static class HrBusinessDate
{
    // Initial Vazin business calendar: Iran. Replace with reviewed company settings before rollout.
    public static DateOnly Today()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
    }
}
