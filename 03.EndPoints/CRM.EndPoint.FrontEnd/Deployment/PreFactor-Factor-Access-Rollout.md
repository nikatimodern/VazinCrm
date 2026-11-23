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

`All` and `Own` below indicate scope *metadata* only. There is no hardcoded
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
4. Do **not** enable a `PreFactor` or `Factor` enforcement flag yet.

```powershell
dotnet ef database update --context UserContextDb --project "02.Infrastructures/CRM.Infrastructures.Sql/Generics.Infrastructures.Sql/CRM.Infrastructures.Sql.csproj" --startup-project "03.EndPoints/CRM.EndPoint.FrontEnd/CRM.EndPoint.FrontEnd.csproj"
```

**Important scope:** This phase registers entities, permissions and role grants
only. It does **not** yet enforce these 21 keys in `FactorController`, cartable,
reports, exports or public customer-approval links. The controller currently
contains many actions with only `[Authorize]` and some specific role checks.
Before enabling enforcement, audit all action paths and associated UI, enforce
record ownership, and keep anonymous customer approval separately protected
by its signed/token-based flow. Existing CRM data tables do not change.
