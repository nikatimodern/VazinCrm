namespace CRM.Core.Domain.AccessCatalog.Entities;

// Scope is independent for every permission. Roles grant Own/All only; specific user
// overrides may include selected owners or the complement of selected owners.
public static class AccessScopes
{
    public const byte None = 0;
    public const byte Own = 1;
    public const byte All = 2;
    public const byte SelectedUsers = 3;
    public const byte AllExceptSelectedUsers = 4;
    // Only entities with an implemented owner resolver may use member selection.
    public static bool SupportsDocumentOwners(string entity) =>
        entity is "PreFactor" or "Factor" or "OrderHead" or "Supply" or "SupplyFactor" or "Accountancy" or "RequestAmount" or "Payment" or "BankAccount" or "Checkbook" or "Check" or "Transfer" or "Cashier" or "Cash" or "BaseCash" or "BaseCenterCash" or "CashGroup" or "Contract" or "Carizo" or "Report" or "CallRecording" or "Contact" or "Warehouse" or "WarehouseProduct" or "WarehouseTransaction" or "WarehouseVoucher" or "Dashboard" or "Agent";
    public static bool SupportsSelectedOwners(string entity) =>
        entity is "Customer" or "Deal" || SupportsDocumentOwners(entity);

    public static bool IsGrant(byte scope) => scope is Own or All;
    public static bool IsOverrideGrant(byte scope) =>
        scope is Own or All or SelectedUsers or AllExceptSelectedUsers;
}
