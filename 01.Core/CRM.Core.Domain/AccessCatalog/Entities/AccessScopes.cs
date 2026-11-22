namespace CRM.Core.Domain.AccessCatalog.Entities;

// 0=effective denied, 1=only assigned/owned records, 2=all records.
// Role grants and user overrides store 1 or 2; deny is IsAllowed=false.
public static class AccessScopes
{
    public const byte None = 0;
    public const byte Own = 1;
    public const byte All = 2;
    public const byte SelectedUsers = 3;
    public static bool IsGrant(byte scope) => scope is Own or All;
    public static bool IsOverrideGrant(byte scope) => scope is Own or All or SelectedUsers;
}
