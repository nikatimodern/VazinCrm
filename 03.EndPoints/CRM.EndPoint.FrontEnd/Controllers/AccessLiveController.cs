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
        var qualitycontrolEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceQualityControlPermissions");
        var qualitycontrol = await DocumentState("QualityControl", qualitycontrolEnforced, ["View", "Create", "Edit", "Delete", "SetContactStatus", "ManageContactHistory"]);
        var qualitycontrolquestionEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceQualityControlQuestionPermissions");
        var qualitycontrolquestion = await DocumentState("QualityControlQuestion", qualitycontrolquestionEnforced, ["View", "Create", "Edit", "Delete"]);
        var productEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceProductPermissions");
        var product = await DocumentState("Product", productEnforced, ["View", "Create", "Edit", "Delete"]);
        var productgroupEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceProductGroupPermissions");
        var productgroup = await DocumentState("ProductGroup", productgroupEnforced, ["View", "Create", "Edit", "Delete"]);
        var productamountEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceProductAmountPermissions");
        var productamount = await DocumentState("ProductAmount", productamountEnforced, ["View", "Create", "Edit", "Delete"]);
        var contractEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceContractPermissions");
        var contract = await DocumentState("Contract", contractEnforced, ["View", "Create", "Edit", "Delete"]);
        var carizoEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCarizoPermissions");
        var carizo = await DocumentState("Carizo", carizoEnforced, ["View", "Create", "Edit", "Delete", "ChangeStatus"]);
        var reportEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceReportPermissions");
        var report = await DocumentState("Report", reportEnforced, ["View", "Create", "Edit", "Delete", "Export"]);
        var callrecordingEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCallRecordingPermissions");
        var callrecording = await DocumentState("CallRecording", callrecordingEnforced, ["View", "Listen"]);
        var chatEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceChatPermissions");
        var chat = await DocumentState("Chat", chatEnforced, ["View", "Create", "Edit", "Delete"]);
        var emailEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceEmailPermissions");
        var email = await DocumentState("Email", emailEnforced, ["View", "MarkRead"]);
        var contactEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceContactPermissions");
        var contact = await DocumentState("Contact", contactEnforced, ["View", "Create", "Edit", "Delete"]);
        var agentEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceAgentPermissions");
        var agent = await DocumentState("Agent", agentEnforced, ["View", "Create", "Edit", "Delete"]);
        var releasenoteEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceReleaseNotePermissions");
        var releasenote = await DocumentState("ReleaseNote", releasenoteEnforced, ["View", "Create", "Edit", "Delete", "Publish", "Archive"]);
        var logsystemEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceLogSystemPermissions");
        var logsystem = await DocumentState("LogSystem", logsystemEnforced, ["View", "ViewDetails"]);
        var systemwarningEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceSystemWarningPermissions");
        var systemwarning = await DocumentState("SystemWarning", systemwarningEnforced, ["View", "Delete"]);
        var companyEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceCompanyPermissions");
        var company = await DocumentState("Company", companyEnforced, ["View", "Create", "Edit", "Delete"]);
        var asettingEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceASettingPermissions");
        var asetting = await DocumentState("ASetting", asettingEnforced, ["View", "Edit"]);
        var baseitemEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceBaseItemPermissions");
        var baseitem = await DocumentState("BaseItem", baseitemEnforced, ["View", "Create", "Edit", "Delete"]);
        var basedescriptionEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceBaseDescriptionPermissions");
        var basedescription = await DocumentState("BaseDescription", basedescriptionEnforced, ["View", "Create", "Edit", "Delete"]);
        var externalsoftwareEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceExternalSoftwarePermissions");
        var externalsoftware = await DocumentState("ExternalSoftware", externalsoftwareEnforced, ["View", "Create", "Edit", "Delete", "ManageSettings"]);
        var externalmappingEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceExternalMappingPermissions");
        var externalmapping = await DocumentState("ExternalMapping", externalmappingEnforced, ["View", "Create", "Edit", "Delete"]);
        var sentsmsEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceSentSmsPermissions");
        var sentsms = await DocumentState("SentSms", sentsmsEnforced, ["View", "Sync"]);
        var receivedsmsEnforced = configuration.GetValue<bool>("AccessCatalog:EnforceReceivedSmsPermissions");
        var receivedsms = await DocumentState("ReceivedSms", receivedsmsEnforced, ["View", "Sync"]);
        var sms = new { view = (byte)(sentsms["view"] > 0 || receivedsms["view"] > 0 ? 2 : 0) };
        var independentCartable = User.IsInRole("مدیر ارشد") || User.IsInRole("کارشناس مالی") || User.IsInRole("مدیر تامین") || User.IsInRole("کارشناس تامین") || User.IsInRole("مدیر عامل");
        var cartable = new { view = (byte)(preFactor["view"] > 0 || factor["view"] > 0 || independentCartable ? 2 : 0) };

        // The role list is read from the database, never from stale cookie claims.
        var roles = (await users.GetRolesAsync(user)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return Json(new
        {
            roles,
            enforced = new { customer = customerEnforced, deal = dealEnforced, prefactor = pfEnforced, factor = factorEnforced, orderhead = orderheadEnforced, supply = supplyEnforced, supplyfactor = supplyfactorEnforced, accountancy = accountancyEnforced, requestamount = requestamountEnforced, payment = paymentEnforced, bankaccount = bankaccountEnforced, checkbook = checkbookEnforced, check = checkEnforced, transfer = transferEnforced, cashier = cashierEnforced, cash = cashEnforced, basecash = basecashEnforced, basecentercash = basecentercashEnforced, cashgroup = cashgroupEnforced, warehouse = warehouseEnforced, warehouseproduct = warehouseproductEnforced, warehousetransaction = warehousetransactionEnforced, warehousevoucher = warehousevoucherEnforced, qualitycontrol = qualitycontrolEnforced, qualitycontrolquestion = qualitycontrolquestionEnforced, product = productEnforced, productgroup = productgroupEnforced, productamount = productamountEnforced, contract = contractEnforced, carizo = carizoEnforced, report = reportEnforced, callrecording = callrecordingEnforced, chat = chatEnforced, email = emailEnforced, contact = contactEnforced, agent = agentEnforced, releasenote = releasenoteEnforced, logsystem = logsystemEnforced, systemwarning = systemwarningEnforced, company = companyEnforced, asetting = asettingEnforced, baseitem = baseitemEnforced, basedescription = basedescriptionEnforced, externalsoftware = externalsoftwareEnforced, externalmapping = externalmappingEnforced, sentsms = sentsmsEnforced, receivedsms = receivedsmsEnforced },
            customer,
            deal,
            prefactor = preFactor,
            factor,
            cartable,
            orderhead, supply, supplyfactor, accountancy, requestamount, payment, bankaccount, checkbook, check, transfer, cashier, cash, basecash, basecentercash, cashgroup, warehouse, warehouseproduct, warehousetransaction, warehousevoucher,
            qualitycontrol, qualitycontrolquestion, product, productgroup, productamount, contract, carizo, report, callrecording, chat, email, contact, agent, releasenote, logsystem, systemwarning, company, asetting, baseitem, basedescription, externalsoftware, externalmapping, sentsms, receivedsms, sms
        });
    }
}
