namespace CRM.Core.ApplicationServices.Customers.Services;

public static class CustomerSearch
{
    private static readonly HashSet<string> StopWords = ["از", "در", "به", "که", "با", "را", "و", "یا", "تا", "برای", "این", "آن"];
    public static string[] Words(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => !StopWords.Contains(word)).Distinct().ToArray();
}
