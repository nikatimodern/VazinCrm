# Selected users scope — AccLevel (development rollout)

The technical model remains `Customer` (ERP «اشخاص»); no Person rename or main-branch edits.

## Data model
- `AccessUserPermissions.Scope = 3`: explicit user-level SelectedUsers override.
- `AccessUserPermissions.Scope = 4`: explicit user-level AllExceptSelectedUsers override.
- `AccessUserScopeMembers`: key `(UserId, AccessPermissionId, TargetUserId)`.
- `Scope = 1` remains Own; `Scope = 2` remains All; deny remains `IsAllowed=false`.
- SelectedUsers adds the **current user's own records implicitly** to those assigned to each chosen target user.
- AllExceptSelectedUsers allows all persons **except** any person assigned to at least one excluded user, including if the person is also assigned to an allowed user. Self cannot be selected as an excluded target.
- Role grants still use Own / All; the targets are different for every user.
- Selections belong to each **permission independently** (View != Edit != Delete). Create remains boolean.
- Only `Customer` currently supports selecting owner users. The model is reusable when other modules acquire an explicit, tested ownership rule.

## Migration — test environment
Apply the prior `20261122210000_AddAccessScopes` migration first (if not already applied).
Then apply `20261122231500_AddSelectedUserAccessScope` and `20261123114500_AddAllExceptSelectedUsersScope` in order to **UserContextDb**:

```bash
dotnet ef database update --context UserContextDb
```

Use the correct startup project/EF project parameters for your Rider solution.
Take a database backup and a manual AccessHistory restore point beforehand.
This migration **does not modify CRM.Customer, UserCustomers, or Person names**.
A database rollback from scope 4 safely becomes Own (1), not All (2), and deletes just that override's exclusion links. A rollback of scope 3 likewise becomes Own.

## Admin UI
1. Open «مدیریت سطح دسترسی» > «مدیریت کاربران».
2. Select the recipient user.
3. For `Customer.View` choose «کاربران انتخابی» **or** «همه به‌جز کاربران انتخابی».
4. The multi-search below that specific permission lets you find employees by name or username. Select target employees (up to 100), independently for each permission.
5. Click «ذخیره دسترسی‌ها». For inclusion, Self is implied; for exclusion, Self cannot be selected and is not excluded. At least one chosen target user is required for either mode.
6. Close and reopen the user to verify chips load from DB.
7. Test removing a chip, switching to Own or All, and «بازگرداندن تغییرات».
8. Undo the change with Access History and inspect the selected users list.
9. If a browser has old cached JS, the server requires `ScopeEditorVersion=3` and refuses stale writes instead of widening scope 4 to All. Hard-refresh the admin page.
10. Switching between inclusion and exclusion deliberately clears the selected users; reselect them before saving.

## Security acceptance tests
- Person-view scope 3 for Ali with Hussein and Mohammad grants only persons associated with Ali, Hussein or Mohammad in `CRM.UserCustomers`.
- Person-view scope 4 for Ali excluding Hussein and Mohammad grants all persons with **no** `UserCustomers` assignment to Hussein or Mohammad. Ali's own person assigned to Hussein too must be denied (deny precedence).
- A newly added employee is included automatically in scope 4 unless the administrator explicitly excludes them.
- If exclusion links go missing/corrupt, scope 4 must fail closed, not become All.
- Verify `Customer.Edit = Own` works independently of `Customer.View = AllExceptSelectedUsers`. Direct GET, edit, and delete endpoints must use the matching permission scope.
- Check that unassigned persons remain visible under scope 4, provided there are no excluded assignments.
- Person-edit scope 1 for Ali still permits edits on Ali's assigned persons **only**, regardless of View scope 3.
- Direct requests for persons related to an **unselected** user return 403/404. Verify list cards, search, phonebook, DataTables and JSON options.
- Removing a selected employee immediately revokes the delegated view after Save.
- Deny overrides any role and either selected-user scope. Switching from inclusion/exclusion to All, Own, Deny, or inherit removes obsolete selection rows on save.
- Search endpoint and write endpoint require manager role; untrusted user IDs, duplicates, malformed GUIDs, inactive permissions, and self-selection are rejected server-side.
- Snapshot restore should preserve exact user selections and undo changes to them. Reopening old snapshots from before this migration must still work.
- With `AccessCatalog:EnforceCustomerPermissions=false`, the old ERP Customer permissions still apply.

## Limitations
- Controllers in other modules still have their previous authorization; this feature does not grant permission to edit or view arbitrary orders/prefactors by the same user.
- This feature was committed remotely; no full .NET build/SQL integration tests were available in the connector environment. Run the build and the acceptance tests before merging.
