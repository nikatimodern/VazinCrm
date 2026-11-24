using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
    checks++;
}
var me = Guid.NewGuid().ToString();
var selected = Guid.NewGuid().ToString();
var other = Guid.NewGuid().ToString();

foreach (var scope in new[] { AccessScopes.Own, AccessScopes.All, AccessScopes.SelectedUsers, AccessScopes.AllExceptSelectedUsers })
{
    Check(AccessScopes.IsOverrideGrant(scope), $"User override accepts {scope}");
    Check(AccessScopes.IsGrant(scope) == (scope is AccessScopes.Own or AccessScopes.All), $"Role grant restricts scope {scope}");
}
Check(!AccessScopes.IsGrant(AccessScopes.None) && !AccessScopes.IsOverrideGrant(AccessScopes.None), "Deny is not a grant");
Check(!AccessScopes.IsOverrideGrant(255), "Invalid scope denied");

var none = new DealOwnerAccess([], []);
var own = new DealOwnerAccess([me], []);
var all = new DealOwnerAccess(null, []);
var include = new DealOwnerAccess([me, selected], []);
var exclude = new DealOwnerAccess(null, [selected]);
var conflict = new DealOwnerAccess([me, selected], [selected]);

Check(!none.Allows(me), "No scope denies own");
Check(own.Allows(me) && !own.Allows(other), "Own isolates other records");
Check(all.Allows(me) && all.Allows(selected) && all.Allows(other), "All includes every owner");
Check(include.Allows(me) && include.Allows(selected) && !include.Allows(other), "Selected users includes only selected and self");
Check(exclude.Allows(me) && !exclude.Allows(selected) && exclude.Allows(other), "All except excludes selected user");
Check(conflict.Allows(me) && !conflict.Allows(selected), "Explicit exclusion wins");
Check(own.Allows(me.ToUpperInvariant()), "Identity matching is case insensitive");
Check(!own.Allows(null), "Own denies missing owner");

foreach (var entity in new[] { "PreFactor", "Factor", "OrderHead", "Supply", "SupplyFactor", "Accountancy",
    "RequestAmount", "Payment", "BankAccount", "Checkbook", "Check", "Transfer", "Cashier", "Cash", "BaseCash",
    "BaseCenterCash", "CashGroup", "Warehouse", "WarehouseProduct", "WarehouseTransaction", "WarehouseVoucher",
    "Contract", "Carizo", "Report", "CallRecording" })
    Check(AccessScopes.SupportsDocumentOwners(entity) && AccessScopes.SupportsSelectedOwners(entity), entity + " owner resolver registered");
foreach (var entity in new[] { "Customer", "Deal" })
    Check(!AccessScopes.SupportsDocumentOwners(entity) && AccessScopes.SupportsSelectedOwners(entity), entity + " uses its dedicated owner resolver");
foreach (var entity in new[] { "Product", "ProductGroup", "ASetting", "Chat", "Email", "SentSms", "ReceivedSms", "Unknown" })
    Check(!AccessScopes.SupportsSelectedOwners(entity), entity + " cannot use arbitrary owner scopes");

Console.WriteLine($"{checks} access catalog domain checks passed. Database, endpoints and live revocation require integration tests.");
