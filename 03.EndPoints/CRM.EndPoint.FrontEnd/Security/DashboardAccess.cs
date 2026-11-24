using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Modules;

namespace CRM.EndPoint.FrontEnd.Security;

public static class DashboardAccess
{
    public static async Task<bool> CanAsync(DocumentAccessPolicy access, ClaimsPrincipal user, string key, CancellationToken ct)
    {
        if (!access.IsEnforced("Dashboard")) return true;
        if (!await access.HasAsync("Dashboard", "View", ct)) return false;
        if (key == "View") return true;
        if (Dashboard.SupportsScope(key))
        {
            if (!await access.HasAnyAsync("Dashboard", key, ct)) return false;
        }
        else if (!await access.HasAsync("Dashboard", key, ct)) return false;

        switch (key)
        {
            case "ViewSales":
                return await access.HasDashboardSourceViewAsync("Factor", ct);
            case "ViewSalesDetails":
                return await CanAsync(access, user, "ViewSales", ct);
            case "ViewOrders":
                return await access.HasDashboardSourceViewAsync("OrderHead", ct);
            case "ViewCustomers":
                return await access.HasDashboardSourceViewAsync("Customer", ct) &&
                    await access.HasDashboardSourceViewAsync("Factor", ct);
            case "ViewReportCount":
            case "ViewReports":
                return await access.HasDashboardSourceViewAsync("Report", ct);
            case "ViewTasks":
                return await access.HasDashboardSourceViewAsync("Carizo", ct);
            case "ViewBankBalance":
            case "ViewReceivables":
            case "ViewPayables":
                // Full-ledger totals are deliberately not calculated from a partial record scope.
                foreach (var entity in new[] { "Accountancy", "Factor", "SupplyFactor", "Payment" })
                    if (!access.IsEnforced(entity) || !await access.HasAsync(entity, "View", ct)) return false;
                return true;
            case "ViewLogs":
                // Do not expose the not-yet-migrated audit-log module through a new grant.
                return access.IsEnforced("LogSystem") && await access.HasAsync("LogSystem", "View", ct);
            case "ViewLogDetails":
                return await CanAsync(access, user, "ViewLogs", ct) &&
                    await access.HasAsync("LogSystem", "ViewDetails", ct);
            default:
                return false;
        }
    }
}
