# AccessCatalog rollout: PreFactor / Factor

Branch: `AccLevel`. Database: **UserConnection / UserContextDb**, not CRMConnection.

## Migration order

1. `20261123234000_RegisterPreFactorFactorAccessEntities`
   - Entity `PreFactor` → `CRM.Core.Domain.PreFactors.Entities.PreFactor`, title «پیش‌فاکتورها»
   - Entity `Factor` → `CRM.Core.Domain.Factors.Entities.Factor`, title «فاکتورها»
   - `DbContextName = CRMContextDb`, since the business data models live there.
2. `20261123234100_RegisterPreFactorFactorPermissions`
   - PreFactor: 13 permissions
   - Factor: 8 permissions
3. `20261123234200_SeedPreFactorFactorRoleGrants`
   - 81 **role-to-permission** grants, automatically inherited by users via
     `AspNetUserRoles`. There is **no copy into AccessUserPermissions**.
   - If an Identity role does not exist on this database, it is skipped.
   - Existing role grants, user overrides, SelectedUsers Scope links, roles,
     user memberships and administrator-customized definitions are not overwritten.

Data migrations are idempotent. Their `Down` is intentionally a no-op so a
migration rollback does not silently delete catalog entries customized by an
administrator. Review the snapshots and remove permissions through the audited UI.

## Permissions

| Entity | Permission keys | Scope-ready |
| --- | --- | --- |
| PreFactor | View, Create, Edit, Delete | View/Edit/Delete |
| PreFactor | AssignTamin, ApproveTamin, ApproveSales, ApproveFinancial | AssignTamin/ApproveTamin |
| PreFactor | SendToCustomer, ApproveCustomerManual, ChangeStatus, EditSupply | SendToCustomer/ChangeStatus/EditSupply |
| PreFactor | ExportExternal | No |
| Factor | View, Create, Edit, Delete | View/Edit/Delete |
| Factor | Approve, Cancel, EditFinancial, ExportExternal | No |

These are distinct workflow permissions, not copies of CRUD:
- `PreFactor.AssignTamin`: `UtaminReg`, supply assignment and re-assignment
- `PreFactor.ApproveTamin`: supply approval/rejection
- `PreFactor.ApproveSales`: `AcceptSalesAdmin`, `NotAcceptSalesAdmin`
- `PreFactor.ApproveFinancial`: `AcceptFinancial`, `NotAcceptFinancial`
- `PreFactor.SendToCustomer`: `SendToCustomer`
- `PreFactor.ApproveCustomerManual`: `AcceptPreFactorManual`
- `PreFactor.ChangeStatus`: `ChangeStatePf`, `Backward`, `ExpirePreFactor`
- `PreFactor.EditSupply`: `UpdatePrefactorItems` and supply-specific edits
- `PreFactor.ExportExternal`: external accounting pre-factor export
- `Factor.Approve` / `Factor.Cancel`: `AcceptFactor` approved / cancelled state
- `Factor.EditFinancial`: `UpdateFactorFinancial`
- `Factor.ExportExternal`: `SendFactorToExternalSoftware` and its preview/mapping flow

## Initial roles (inherited by current users)

`All` and `Own` below are enforced when the corresponding module flag is enabled. There is no hardcoded
UserId anywhere; assigning/removing a role automatically changes a user's
inherited rights. Existing explicit user overrides still take precedence.

| Role | PreFactor | Factor |
| --- | --- | --- |
| مدیر ارشد | all 13 keys (All) | all 8 keys (All) |
| مدیر عامل | View, ApproveSales, ExportExternal (All) | View, ExportExternal (All) |
| مدیر اجرایی | View, AssignTamin, ChangeStatus (All) | View (All) |
| مدیر فروش | View, Create, Edit, Delete, AssignTamin, ApproveSales, SendToCustomer, ApproveCustomerManual, ChangeStatus (All) | View, Create, Edit (All) |
| کارشناس فروش | View/ Edit/ SendToCustomer (Own), Create (All) | View/ Edit (Own), Create (All) |
| مدیر تامین | View, AssignTamin, ApproveTamin, EditSupply, ChangeStatus (All) | View (All) |
| کارشناس تامین | View, ApproveTamin, EditSupply (Own) | View (Own) |
| مدیر مالی | View, ApproveFinancial, ExportExternal (All) | all 8 keys (All) |
| کارشناس مالی | View, ApproveFinancial, ExportExternal (All) | all 8 keys (All) |

The owner policy for future enforcement needs to be reviewed carefully:
`PreFactor.UserCreatorID` identifies the sales creator, whereas
`PreFactor.UserTaminId` identifies the supply assignee. `Factor` has
`UserCreatorID` and `UserTaminId` too. `Own` must be evaluated against the
appropriate field for each action, not blindly only the creator.
Customer or Supplier Identity roles are not given ERP-wide grants by this migration.

## Execute and validate

1. Back up **UserConnection** and take an AccessCatalog restore point.
2. Apply pending migrations on test/staging, then verify with:
   `Deployment/VerifyPreFactorFactorAccessCatalog.sql` (READ-ONLY).
3. Review all 21 keys in AccessCatalogAdmin, inspect role counts and effective
   user permissions, especially users who have multiple roles or personal overrides.
4. Build the solution, then enable the two flags on test/staging only after the grants have been verified:

```json
{
  "AccessCatalog": {
    "EnforcePreFactorPermissions": true,
    "EnforceFactorPermissions": true
  }
}
```

The flags default to false when absent. Do not enable in production before the regression checks below.

**SSMS alternative:** Run `Deployment/RegisterPreFactorFactorAccessCatalog.sql`
on `UserConnection`. It starts with `@Apply = 0` and only previews the
current catalog and existing role memberships; review that output, then set
`@Apply = 1` and run it again to commit all three phases in one SQL
transaction. Re-running or applying the matching EF data migrations afterwards
does not duplicate permissions or role grants. SQL changes are not captured as
application history; take a restore point and database backup beforehand.

```powershell
dotnet ef database update --context UserContextDb --project "02.Infrastructures/CRM.Infrastructures.Sql/Generics.Infrastructures.Sql/CRM.Infrastructures.Sql.csproj" --startup-project "03.EndPoints/CRM.EndPoint.FrontEnd/CRM.EndPoint.FrontEnd.csproj"
```

## Enforcement and regression checks

- `DocumentAccessPolicy` resolves effective role/user grants and selected/excluded scopes.
- PreFactor view includes sales creator and assigned supply expert; supply-specific actions use the supply owner. Factor view includes seller, creator and supply assignee. Factor edit/delete use seller or creator. Exclusions win over inclusions.
- Main FactorController CRUD, approvals, state changes, prints/downloads, export and related APIs check document permissions. Cartable queries, Excel, customer document lists and deal document counts apply server-side scope filters.
- Cards and document editor handlers check distinct permissions. AccessLive reloads affected tabs or locks an open editor on permission changes, preserving the existing Dashboard restriction message. Other independent cartable workflows remain available under their legacy roles.
- Customer portal document reads are person-bound. The public SMS approval page retains the legacy link format, then uses a document-bound protected token for preview and approval. Internal sessions must also satisfy the corresponding access check.

Test with separate sales, supply, finance and admin accounts. For each of the 21 keys, verify allowed/denied direct endpoint calls, scope Own/All/SelectedUsers/AllExceptSelectedUsers where supported, and explicit user deny over inherited role grants. Attempt forged document IDs, item/description IDs, approval fields and ownership changes. Verify lists, counts, Excel, print, linked attachments, approvals, cancellation, and Sepidar operations. Test revocation with an open form and two browser tabs, including both documents denied and only one denied. Test the customer SMS link and OTP receipt upload separately.

Remaining rollout caveat: shared financial/reporting, allocation, order, QC and external integration endpoints that operate on documents through their own entities have not all been converted to the new policies. Audit those paths and run full .NET/Razor compilation and SQL Server integration tests before enabling in production. The existing 7-character public SMS link is legacy and should be replaced with a stronger opaque token in a separate rollout. No production configuration or database was changed by these source commits.

Keep the separate registration and verification SQL scripts until the consolidated all-entity script is assembled.
