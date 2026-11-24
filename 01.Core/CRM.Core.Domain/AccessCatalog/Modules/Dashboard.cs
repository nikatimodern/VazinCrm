namespace CRM.Core.Domain.AccessCatalog.Modules;

// A catalog module, not a persisted CRM entity.
public static class Dashboard
{
    public static readonly string[] ScopedKeys = ["ViewSales", "ViewSalesDetails", "ViewOrders", "ViewCustomers", "ViewReportCount"];
    public static readonly string[] GlobalKeys = ["View", "ViewReports", "ViewBankBalance", "ViewReceivables", "ViewPayables", "ViewTasks", "ViewLogs", "ViewLogDetails"];
    public static bool SupportsScope(string key) => System.Array.IndexOf(ScopedKeys, key) >= 0;
}
