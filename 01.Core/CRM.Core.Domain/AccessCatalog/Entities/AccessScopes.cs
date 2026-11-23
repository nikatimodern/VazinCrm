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
    public static bool IsGrant(byte scope) => scope is Own or All;
    public static bool IsOverrideGrant(byte scope) =>
        scope is Own or All or SelectedUsers or AllExceptSelectedUsers;
}
