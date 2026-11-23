using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Controllers;

/// <summary>
/// Rechecks the signed-in user's real database grants after a SignalR access event.
/// SignalR is only an invalidation hint; this endpoint remains authoritative.
/// </summary>
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccessLiveController(
    UserManager<AppUser> users, SignInManager<AppUser> signInManager,
    IAccessScopeService scopes, IConfiguration configuration) : Controller
{
    [HttpGet]
    public Task<IActionResult> State(string? module, CancellationToken ct) => ReadStateAsync(module, ct);

    // Refresh the Identity principal as well: role changes must not rely on
    // the old role claims carried by an already-open browser tab.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(string? module, CancellationToken ct)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        await signInManager.RefreshSignInAsync(user);
        return await ReadStateAsync(module, ct);
    }

    private async Task<IActionResult> ReadStateAsync(string? module, CancellationToken ct)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var id = user.Id;
        var customerEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions");
        var dealEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceDealPermissions");

        async Task<byte> Scope(string entity, string key, bool enforced) =>
            enforced ? await scopes.GetEffectiveScopeAsync(id, entity + "." + key, ct) : AccessScopes.All;

        // Do not query every catalog permission on every ERP page load.
        // Only the currently opened module needs a live access matrix.
        // The two View grants also control their navigation links, regardless
        // of which page is currently open. Detail scopes are module-local.
        var customerView = await Scope("Customer", "View", customerEnforced);
        var dealView = await Scope("Deal", "View", dealEnforced);
        object customer = new { view = customerView };
        if (string.Equals(module, "customer", StringComparison.OrdinalIgnoreCase))
        {
            var cc = await Scope("Customer", "Create", customerEnforced);
            var ce = await Scope("Customer", "Edit", customerEnforced);
            var cd = await Scope("Customer", "Delete", customerEnforced);
            var ca = await Scope("Customer", "Approve", customerEnforced);
            var cm = await Scope("Customer", "ManageExperts", customerEnforced);
            var cs = await Scope("Customer", "AssignSelf", customerEnforced);
            customer = new { view = customerView, create = cc, edit = ce, delete = cd,
                approve = ca, manageExperts = cm, assignSelf = cs };
        }

        object deal = new { view = dealView };
        if (string.Equals(module, "deal", StringComparison.OrdinalIgnoreCase))
        {
            var dc = await Scope("Deal", "Create", dealEnforced);
            var de = await Scope("Deal", "Edit", dealEnforced);
            var dd = await Scope("Deal", "Delete", dealEnforced);
            var ds = await Scope("Deal", "ChangeStatus", dealEnforced);
            var da = await Scope("Deal", "Assign", dealEnforced);
            var df = await Scope("Deal", "ApproveFinancial", dealEnforced);
            var dm = await Scope("Deal", "ManageFollowUp", dealEnforced);
            deal = new { view = dealView, create = dc, edit = de, delete = dd,
                changeStatus = ds, assign = da, approveFinancial = df, manageFollowUp = dm };
        }

        // The role list is read from the database, never from stale cookie claims.
        var roles = (await users.GetRolesAsync(user)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return Json(new
        {
            roles,
            enforced = new { customer = customerEnforced, deal = dealEnforced },
            customer,
            deal
        });
    }
}
