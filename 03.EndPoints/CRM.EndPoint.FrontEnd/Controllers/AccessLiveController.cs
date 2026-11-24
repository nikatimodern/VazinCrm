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

        var pfEnforced = configuration.GetValue<bool>("AccessCatalog:EnforcePreFactorPermissions");
        var factorEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceFactorPermissions");
        async Task<System.Collections.Generic.Dictionary<string, byte>> DocumentState(string entity, bool enabled, string[] keys)
        {
            var values = new System.Collections.Generic.Dictionary<string, byte>();
            foreach (var key in keys)
                values[char.ToLowerInvariant(key[0]) + key[1..]] = await Scope(entity, key, enabled);
            return values;
        }
        // These documents are embedded in Cartable, Deal and Customer pages.
        var preFactor = await DocumentState("PreFactor", pfEnforced, ["View", "Create", "Edit", "Delete", "AssignTamin", "ApproveTamin", "ApproveSales", "ApproveFinancial", "SendToCustomer", "ApproveCustomerManual", "ChangeStatus", "EditSupply", "ExportExternal"]);
        var factor = await DocumentState("Factor", factorEnforced, ["View", "Create", "Edit", "Delete", "Approve", "Cancel", "EditFinancial", "ExportExternal"]);
        var orderheadEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceOrderHeadPermissions");
        var orderhead = await DocumentState("OrderHead", orderheadEnforced, ["View", "Create", "Edit", "Delete", "ChangeStatus"]);
        var supplyEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceSupplyPermissions");
        var supply = await DocumentState("Supply", supplyEnforced, ["View", "Create", "Edit", "Delete"]);
        var supplyfactorEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceSupplyFactorPermissions");
        var supplyfactor = await DocumentState("SupplyFactor", supplyfactorEnforced, ["View", "Create", "Edit", "Delete", "ManageAllocations", "ExportExternal"]);
        var accountancyEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceAccountancyPermissions");
        var accountancy = await DocumentState("Accountancy", accountancyEnforced, ["View", "Create", "Edit", "Delete", "Approve", "ChangeStatus", "Settle", "Transfer", "UseWallet"]);
        var requestamountEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceRequestAmountPermissions");
        var requestamount = await DocumentState("RequestAmount", requestamountEnforced, ["View", "Create", "Edit", "Delete", "Approve", "ChangeStatus", "ChangeDocType", "Pay"]);
        var paymentEnforced = configuration.GetValue<bool>("AccessCatalog:EnforcePaymentPermissions");
        var payment = await DocumentState("Payment", paymentEnforced, ["View", "Create", "Edit", "Delete"]);
        var bankaccountEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceBankAccountPermissions");
        var bankaccount = await DocumentState("BankAccount", bankaccountEnforced, ["View", "Create", "Edit", "Delete", "ChangeActive"]);
        var checkbookEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCheckbookPermissions");
        var checkbook = await DocumentState("Checkbook", checkbookEnforced, ["View", "Create", "Edit", "Delete"]);
        var checkEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCheckPermissions");
        var check = await DocumentState("Check", checkEnforced, ["View", "Create", "Edit", "Delete", "ChangeStatus"]);
        var transferEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceTransferPermissions");
        var transfer = await DocumentState("Transfer", transferEnforced, ["View", "Create", "Edit", "Delete", "ChangeStatus", "Approve"]);
        var cashierEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCashierPermissions");
        var cashier = await DocumentState("Cashier", cashierEnforced, ["View", "Create", "Edit", "Delete"]);
        var cashEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCashPermissions");
        var cash = await DocumentState("Cash", cashEnforced, ["View", "Create", "Edit", "Delete", "ChangeStatus", "Approve", "ExportExternal"]);
        var basecashEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceBaseCashPermissions");
        var basecash = await DocumentState("BaseCash", basecashEnforced, ["View", "Create", "Edit", "Delete"]);
        var basecentercashEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceBaseCenterCashPermissions");
        var basecentercash = await DocumentState("BaseCenterCash", basecentercashEnforced, ["View", "Create", "Edit", "Delete"]);
        var cashgroupEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCashGroupPermissions");
        var cashgroup = await DocumentState("CashGroup", cashgroupEnforced, ["View", "Create", "Edit", "Delete", "Approve", "Export"]);
        var warehouseEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceWarehousePermissions");
        var warehouse = await DocumentState("Warehouse", warehouseEnforced, ["View", "Create", "Edit", "Delete", "ManageUsers"]);
        var warehouseproductEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceWarehouseProductPermissions");
        var warehouseproduct = await DocumentState("WarehouseProduct", warehouseproductEnforced, ["View", "Create", "Edit", "Delete"]);
        var warehousetransactionEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceWarehouseTransactionPermissions");
        var warehousetransaction = await DocumentState("WarehouseTransaction", warehousetransactionEnforced, ["View", "Create", "Edit", "Delete"]);
        var warehousevoucherEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceWarehouseVoucherPermissions");
        var warehousevoucher = await DocumentState("WarehouseVoucher", warehousevoucherEnforced, ["View", "Create", "Edit", "Delete", "Approve", "Deliver"]);
        var independentCartable = User.IsInRole("مدیر ارشد") || User.IsInRole("کارشناس مالی") || User.IsInRole("مدیر تامین") || User.IsInRole("کارشناس تامین") || User.IsInRole("مدیر عامل");
        var cartable = new { view = (byte)(preFactor["view"] > 0 || factor["view"] > 0 || independentCartable ? 2 : 0) };

        // The role list is read from the database, never from stale cookie claims.
        var roles = (await users.GetRolesAsync(user)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return Json(new
        {
            roles,
            enforced = new { customer = customerEnforced, deal = dealEnforced, prefactor = pfEnforced, factor = factorEnforced, orderhead = orderheadEnforced, supply = supplyEnforced, supplyfactor = supplyfactorEnforced, accountancy = accountancyEnforced, requestamount = requestamountEnforced, payment = paymentEnforced, bankaccount = bankaccountEnforced, checkbook = checkbookEnforced, check = checkEnforced, transfer = transferEnforced, cashier = cashierEnforced, cash = cashEnforced, basecash = basecashEnforced, basecentercash = basecentercashEnforced, cashgroup = cashgroupEnforced, warehouse = warehouseEnforced, warehouseproduct = warehouseproductEnforced, warehousetransaction = warehousetransactionEnforced, warehousevoucher = warehousevoucherEnforced },
            customer,
            deal,
            prefactor = preFactor,
            factor,
            cartable,
            orderhead, supply, supplyfactor, accountancy, requestamount, payment, bankaccount, checkbook, check, transfer, cashier, cash, basecash, basecentercash, cashgroup, warehouse, warehouseproduct, warehousetransaction, warehousevoucher
        });
    }
}
