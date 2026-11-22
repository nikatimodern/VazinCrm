# Per-permission scope rollout — AccLevel

## Migration
Run on **UserConnection / UserContextDb** only, after a database backup:
```powershell
Update-Database -Context UserContextDb
```
Last added migration: `20261122210000_AddAccessScopes`.

The migration adds `SupportsScope` to `AccessPermissions`, and
`Scope` to `AccessRolePermissions` and `AccessUserPermissions`.
Scope values: **0 = effective deny**, **1 = own/assigned**, **2 = all**.
Stored grants use only 1 or 2; explicit user deny remains `IsAllowed=false`.

**Existing role grants and user allows are migrated to All (2), not Own (1).**
New write requests must include each scope explicitly. Old cached browser clients that omit
scopes are rejected instead of silently elevating an Own grant to All; refresh the page.
Previously configured overrides and roles are preserved; no Identity role
or user membership table is reset.

## New management UI
- Permissions can opt into Own/All. Standard View/Edit/Delete permissions are enabled by default;
  Create and custom operations are boolean unless opted in.
- Role editor: select a permission, then Own or All when supported.
- User editor: inherit roles, allow Own, allow All, or deny.
- Effective scope = widest assigned role scope; explicit user override replaces it.
- Scope is included in history snapshots and restores, including backward-compatibility
  for older historical entries that had no Scope (their grants mean All).
- When deleting a role, selecting **keep** retains the actual scope from the role.

## Safe activation
The evaluator `IAccessScopeService` is registered with scoped lifetime:
- `GetEffectiveScopeAsync(userId, "Customer.View", ct)` returns 0 / 1 / 2.
- `CanAccessCustomerAsync(userId, "Customer.Edit", customerId, ct)`
  checks `UserCustomers` for Own and allows directly for All.
- Inactive, unregistered, invalid and unsupported policies **fail closed**.

**Important: these new policies are NOT YET enforced automatically by controllers,
queries, menus or mutations.** Existing legacy role/policy checks remain in effect.
Do not claim a user's effective scope in the administration screen is already a
server-side authorization boundary. Migrate one module at a time, and make sure
every read/search/export/detail/mutation path is guarded before enabling it.

## Suggested regression tests (test database)
1. Role granted View before migration still resolves All; no unplanned privilege loss.
2. Assign View=Own to one role, View=All to another; user with both resolves All.
3. Assign user deny: resolves None, regardless of roles.
4. User Own override supersedes role All; user All override supersedes role Own.
5. Own matches only UserCustomers assignments for the target customer; fake/non-GUID
   IDs are denied.
6. Delete a role with an Own grant and choose keep; retained explicit scope is Own.
7. Save history, modify scopes, restore one change and point restore; verify scope stays exact.
8. Older snapshots created before migration must still preview and restore.
9. Check that unregistered permissions, inactive entities and keys from a different entity
   do not grant access.
10. Inspect legacy ASP.NET role checks before renaming/deleting their Identity roles.

No changes to `main` should be made during these checks.
