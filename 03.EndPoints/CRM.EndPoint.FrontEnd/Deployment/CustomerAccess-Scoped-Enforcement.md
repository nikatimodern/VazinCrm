# Scoped authorization for اشخاص (Customer) — AccLevel

This implementation keeps the technical entity/table name `Customer`. It does **not**
rename anything to `Person` and does not modify `main`.

## Opt-in activation (test environment first)

Ensure `20261122210000_AddAccessScopes` was applied in `UserContextDb`.
No additional migration is required for this CustomerController change.

Set the following only in the **test** environment's configuration:

```json
{
  "AccessCatalog": {
    "EnforceCustomerPermissions": true
  }
}
```

Alternatively set environment variable
`AccessCatalog__EnforceCustomerPermissions=true`.
There is intentionally **no production appsettings change** in this branch.
When unset/false, the old CustomerController role rules remain active.

**Before enabling**, register access entity `Customer` with active
`View`, `Create`, `Edit`, `Delete` permissions, assign the administrator
needed grants (usually `All`), verify that the admin retains its identity
role, and create a manual restore point. Missing/inactive grants fail closed.

## Permissions

| Key | Enforcement |
| --- | --- |
| Customer.View | Directory, search suggestions, phonebook, tables, details and person profile. Own filters SQL queries using `UserCustomers`. |
| Customer.Create | Create a person. Binary, not Own/All. |
| Customer.Edit | Update a person; Own checks the person-user assignment. Administrative assignment, approval and self-assignment currently require All. |
| Customer.Delete | Reject/delete person; Own checks assignment before processing. |

Role union and explicit overrides follow `IAccessScopeService` semantics.
A user's `Deny` overrides all roles; an explicit `Own` overrides role `All`.

Scope **Own** currently means `CRM.UserCustomers.CustomerId = Customer.Id`
and `UserCustomers.UserId = logged-in user's GUID`.
Do not assume supplier responsibility or arbitrary `UserCreatorId` is equivalent
to this mapping. Verify supply-side ownership before reusing this rule elsewhere.

## Read protections and special cases

- `GetAllCustomerPartial`, `SearchAllCustomerPartial`: SQL-side ownership
  filtering; sales managers with Own cannot bypass it via their legacy role.
- Juridical/non-juridical DataTables: SQL filter before `Count` and pagination,
  so counts and pages do not expose other persons.
- `GetCustomerOp`, `GetSuppliers`, `GetCustomersList`, `GetPhonebook`,
  duplicate suggestions: ownership filter in query, not presentation only.
- `GetCustomer`, `GetCustomerByMobile`, `CustomerDetail`,
  `CustomerProfile`, `GetCustomerRel`, experts: resource-specific checks.
- `GetAllPreFactorPartial`: validates the actual person linked to `DealId`
  and filters prefactors by that person; mismatched `CustomerId` is denied.
- `GetSepidarFactors`: **All** View required while the serial-number lookup
  cannot prove which person the external invoice belongs to.
- `GetUsersForPreFactor`, `ChangeUsersPreFactor`, `NormalizeAllCustomers`:
  retain the senior-admin Identity role restriction (in addition to scoped
  checks), pending dedicated PreFactor/system permissions.
- The user-facing directory hides edit/delete actions when scope is missing
  or the record is not assigned. This is purely UX: controllers and commands
  make the actual authorization decision.
- `GetUsersRel` is not an HTTP action.

`AbortCustomer`, `AcceptCustomer` and `AddToMyCustomer` have been changed
from GET to **POST** with `ValidateAntiForgeryToken`. The directory JS now
sends the matching token. Any non-browser caller must migrate accordingly.
This change does not yet convert every other legacy write route to CSRF-protected POST.

## Test matrix (staging only)

1. With the flag **off**, verify existing list/edit/profile behaviour and role
   restrictions, including login and registration, before changing roles.
2. With the flag **on** and an admin granted all four permissions, verify
   create/read/update/delete, customer acceptance and assignment workflows.
3. View=Own user: assigned records appear in directory, options, tables,
   summary and phonebook; unassigned records never appear.
4. View=Own user: direct URL/API requests for an unassigned person's ID or
   mobile must return 403/404, not person details.
5. View=All + Edit=Own: user can browse all but the edit button and API
   cannot edit another user's record, even with a forged ID/body.
6. Delete=Own: an assigned person can be rejected/deleted if authorized;
   unassigned person cannot.
7. Create-only, View-only, and Deny-overrides-role users must have the expected
   restrictions. Test multiple roles (Own + All yields All).
8. A user with only Own Edit cannot assign arbitrary persons to themself or
   alter expert assignments. Only Edit=All can do so.
9. For a person-related prefactor, a forged `DealId` / `CustomerId`
   combination must be denied; Own must never return other persons' prefactors.
10. Old mutation GET calls must be rejected; updated POST calls with valid
    anti-forgery token should work.
11. Disable the flag and verify immediate legacy fallback without database changes.
12. Verify the scope history/restore tests from the previous stage still pass.

### Important rollout boundaries
- The person controller and its write command handlers have been adapted;
  **other controllers and modules that independently query person data
  are not automatically gated**. Audit them before turning on this policy
  globally or claiming system-wide Person access enforcement.
- This branch does not alter the `Person` rename work in any other branch.
- A full `.NET` build, web integration tests, and SQL integration tests must
  run in the user's environment; GitHub here has no Actions runs and this
  workspace has no `dotnet` SDK.
