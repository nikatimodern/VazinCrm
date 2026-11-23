# Customer / Deal permission enforcement (AccLevel)

## Prerequisites and safe rollout

1. Apply all **UserContextDb** migrations, including
   `20261123114500_AddAllExceptSelectedUsersScope`.
2. Run `AccLevel_Customer_Deal_Permissions_Backfill.sql` against the **UserConnection**
   database, first with `@Apply=0`, then on staging with `@Apply=1`.
   Back up the database, review role scopes and personal deny overrides, and
   create an AccessCatalog restore point before the SQL run. Direct SQL does not
   create application history automatically.
3. Confirm the **Customer** and **Deal** entities, all 15 permissions, and
   role/user grants in AccessCatalogAdmin.
4. Enable the policies **only on staging**:
   ```json
   { "AccessCatalog": {
       "EnforceCustomerPermissions": true,
       "EnforceDealPermissions": true
   }}
   ```
   Until switched on, each module preserves its legacy controller behavior;
   the catalog by itself does **not** enforce access. Do not change production
   configuration or merge into main without staging tests.

## Implemented permissions

| Key | New server-side enforcement |
| --- | --- |
| Customer.View | Lists, search, details, profiles and person-related lookups; SQL owner filters |
| Customer.Create | Create person |
| Customer.Edit | Update assigned person |
| Customer.Delete | Reject / delete assigned person |
| Customer.Approve | Approve person; no longer piggybacks on Edit All |
| Customer.ManageExperts | Assign/remove person-expert relationships; unrestricted grant required |
| Customer.AssignSelf | Claim a person; requires unrestricted View to prevent a scope bypass |
| Deal.View | Index, GetAllDeals, GetAllDealsDt, GetDeal, overdue lists/details |
| Deal.Create | AddDeal for new deals |
| Deal.Edit | AddDeal for existing deals; restores privileged fields from persisted record |
| Deal.Delete | DeleteDeal and related soft-delete |
| Deal.ChangeStatus | ChangeStatusDeal, drag/drop and overdue follow-up status transitions |
| Deal.Assign | AssignUserSuspendDeal (plus Customer.ManageExperts if it changes UserCustomers) |
| Deal.ApproveFinancial | ToggleFinancialStatusDeal |
| Deal.ManageFollowUp | OverdueDeal.UpdateFollowUp and per-record follow-up control |

The POST action `Customer/NormalizeAllCustomers` rebuilds search-normalization
fields for persons. It is available to **any authenticated user** and has no
`Customer.Normalize` permission or Identity role gate. It is a database-wide
maintenance operation, not a permission in the AccessCatalog.

The migration `20261123153000_RemoveCustomerNormalizePermission` removes the
obsolete permission and its role/user grants from existing UserContextDb
databases. Older copies of `AccLevel_Customer_Deal_Permissions_Backfill.sql`
that still insert `Customer.Normalize` must not be run again without removing
that permission from their seed data, or they will recreate it. Historical
AccessCatalog restore points may also contain the retired permission.

## Ownership semantics

- Customer uses `UserCustomers` (many-to-many). A person assigned to any excluded
  owner is denied even when also assigned to an allowed owner.
- Deal uses `Deal.UserCreatorID` (the responsible Identity user's string ID).
  Assignment changes ownership. Unassigned deals are included in AllExcept,
  excluded only when matching an explicitly excluded owner.
- Values: `0=None`, `1=Own`, `2=All`, `3=SelectedUsers`,
  `4=AllExceptSelectedUsers`. Scope 3 includes self; scope 4 excludes the
  selected owners. For scopes 3/4, a missing or invalid selection denies access.
- Only `Customer` and `Deal` support per-user selected-user scopes.
  Role grants remain Own or All. User overrides take precedence over role grants;
  explicit denial always wins.
- Selected user scopes are independently validated in the permission editor,
  repository, effective-scope service, and history restore.

## Deleting a catalog permission that has assignments

- The `PermissionRemovalPreview` endpoint reports how many role grants,
  direct user overrides and selected-user Scope memberships reference the permission.
- The UI explicitly warns that removing a role grant also removes that permission
  from every user inheriting it through the role.
- `DeletePermission` is an admin-only, anti-forgery-protected POST. It requires an
  affirmative confirmation and the counts shown in the preview; old POST calls
  with only an ID are rejected.
- Deletion removes selected Scope members, user overrides, role grants and the
  permission within one SQL Server serializable transaction.
- If any dependency count changes between preview and execution, the operation
  is cancelled and the user must review and confirm again.
- Normal AccessCatalog history retains before/after snapshots for audit and
  recovery. No Identity role membership or unrelated permission is deleted.
- Regression checks: cancel confirmation (no writes); delete unassigned grant;
  delete permission with roles/users/selected members; stale preview (409); 
  permissions of other entities remain intact; inspect history after deletion.

## Direct API checks on staging

1. **Role union**: two roles grant Own and All -> All. Explicit user Deny overrides
   either. An explicit Own override must reduce an inherited All.
2. **Scoped lists**: as user A, `Deal.View=Own` shows only A's deals on
   `GetAllDeals`, successful-deal DataTables and overdue tables, with filtered
   counts; requests with another user's `EntityId` cannot expose that deal.
3. **Selected / excluded**: include B, then exclude B; direct GET by ID must
   follow the selection. In Customer, a shared person linked to B must always
   be excluded. For Deal, an order is owned by exactly one responsible user.
4. **Independent actions**: Deal.View=All, Deal.Edit=Own, Deal.ChangeStatus=Own,
   Deal.Delete=Deny, Deal.Assign=Deny. Try forged JSON/IDs against
   AddDeal, ChangeStatusDeal, DeleteDeal, AssignUserSuspendDeal and
   ToggleFinancialStatusDeal: disallowed calls must return 403/409.
5. **Mass-assignment**: forged AddDeal JSON with
   `UserCreatorID`, `Status`, `FinancialStatus`, `IsDeleted`,
   `Suspend` must not change those persisted values when Deal policy is on.
   Newly created deals must start in the default status and unapproved state.
6. **Cross-module**: editing a deal without Customer.Edit must not update
   linked customer's stored data nor cascade updates to invoices/prefactors.
   Deal.Assign must not create a UserCustomers link without
   Customer.ManageExperts.
7. **Financial lock**: approved deals cannot be edited/deleted/change status
   by bypassing the front end.
8. **Overdue**: view restricted by Deal.View. Follow-up restricted by
   Deal.ManageFollowUp and, for a status transition, Deal.ChangeStatus.
   Changing to Success without an invoice must be rejected; marking Failed
   with an invoice must be rejected.
9. **Customer granular keys**: Customer.Approve without Customer.Edit,
   and Customer.ManageExperts without Customer.Edit must perform only their
   designated operation. Old Edit All alone must not grant approval or expert
   changes once enforcement is on.
10. **Legacy fallback**: disable both flags and compare behavior with the
    previous branch. Verify role-only endpoints, token logins and integrations.
11. **No grants / inactive catalog**: disabled entities, unknown permissions
    and missing selected members must deny access.

### Limits and follow-up hardening

- Other ERP controllers that query or change Deal/Customer and their related
  Factor, PreFactor, Accountancy, SupplyFactor and OrderHead records do not
  automatically inherit these policies. They need their own audits.
- Existing Deal mutation routes can still accept GET for backward compatibility;
  plan a coordinated migration to CSRF-protected POST and update all callers.
- The person and deal policies are separately opt-in to avoid unintended
  production lockouts. Always enable only after applying the catalog data.
- A complete .NET build, Razor compilation, and SQL integration tests are
  required in the deployment environment; this source-only change has not
  been built or executed against the production database.


## Live permission revocation (SignalR)

- Any successful AccessCatalog admin write (`SaveUserAccess`, `SaveUserProfile`,
  `SaveRolePermissions`, role/catalog changes and permission deletion) publishes
  an `AccessPermissionsChanged` event only after the write/history operation
  succeeds. Restoring access history publishes the same event.
- Personal user changes use `IHubContext<TaskHub>.Clients.User(userId)`, which
  delivers to every open tab/device for the user. Changes to roles/catalog
  broadcast an anonymous invalidation hint; no grant details are sent over SignalR.
- `/AccessLive/Sync` is authenticated and anti-forgery protected. It refreshes
  the Identity sign-in ticket, then obtains current effective database scopes.
  `/AccessLive/State` obtains the initial baseline without changing the cookie.
- The current page's module receives the full action scope matrix. Both
  `Customer.View` and `Deal.View` are checked on every page to revalidate
  the menu. `MenuService` excludes their links if View is denied and
  enforcement is enabled.
- The browser reloads the current page (or redirects from a revoked View page)
  when there is no open/modified form. A form already being edited is preserved;
  stale submit/save actions are disabled and a reload warning is shown.
  Customer/Deal create/update submit handlers also check the live state.
  API/controller guards continue to independently enforce every action.
- After a role change and a session refresh, the TaskHub connection reconnects
  so its role-based SignalR group membership no longer uses old cookie claims.
  A normal socket reconnection also rechecks access to cover missed messages.
- Other entities automatically receive this generic invalidation and shell
  revalidation behavior, but **per-action controls** are only wired for
  `Customer` and `Deal` until the other controllers are converted to the
  AccessCatalog. This mechanism does not grant/deny backend access by itself.

### Required staging verification

1. Sign in as user B in two different browser tabs (and optionally another device).
   As an admin, remove `Customer.View`: both tabs receive the event and leave
   the denied page; Customer is absent from the refreshed menu.
2. Remove `Deal.Create` with the Deal modal open. Its inputs remain intact;
   save is disabled and the backend rejects any direct forged POST.
3. Change `Deal.View` from All to Own, or modify selected/excluded owners
   without changing the numeric scope. In an unedited tab, the records are
   reloaded using the new ownership filter.
4. Remove a role with related users, change a role's grants, then restore a
   history point. Verify the user's roles and menus refresh, and notifications
   sent to role groups target current members after reconnection.
5. Temporarily disable SignalR, change a permission, then restore connection.
   Verify reconnect-triggered revalidation; backend checks must continue to
   reject disallowed writes throughout the disconnection.
6. Confirm catalog edits do not reload the administrator's current
   AccessCatalog screen, and unrelated users with unchanged state are not
   forced to reload.

No automated browser or SQL Server staging test is implied by these notes.
