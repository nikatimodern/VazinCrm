# OrderHead, Supply, SupplyFactor, Accountancy rollout
Branch: AccLevel. UserConnection / UserContextDb only for catalog scripts.

Registration: `RegisterOrderSupplyAccountancyAccessCatalog.sql` (default @Apply=0) or migration `20261124234500_RegisterOrderSupplyAccountancyAccess`. They are idempotent inserts; do not overwrite role grants or explicit overrides. Keep separately for the eventual AccessCatalog_CompleteSetup.sql.

| Entity | Keys |
| --- | --- |
| OrderHead | View, Create, Edit, Delete, ChangeStatus |
| Supply | View, Create, Edit, Delete |
| SupplyFactor | View, Create, Edit, Delete, ManageAllocations, ExportExternal |
| Accountancy | View, Create, Edit, Delete, Approve, ChangeStatus, Settle, Transfer, UseWallet |

24 keys and 123 proposed role grants. Role memberships determine inheritance. Check local role names and grants in the preview before applying. No customer or supplier ERP-wide grants.

Scope: OrderHead uses creator or assigned supply expert. Supply uses creator or linked pre-factor supply expert. SupplyFactor and Accountancy use record creator. Selected/excluded user filters are supported for scoped keys, exclusions take priority. Creation and financial export are unscoped. The policy is request-local, shared by the main controllers, repositories and Razor actions.

Primary integration: OrderController, SupplyController (including financial export partial), AccountancyController, payment writes in FactorController, order/supply/accountancy read repositories, AccessLiveController and live editor locks. Independent banking, payment and warehouse modules remain under their existing policies.

## Staging checklist
- Apply the data migration or SQL after backing up UserConnection; verify the 4 entities and 24 permissions; check role inheritance/individual deny and all four supported scopes.
- Build the solution including Razor. Test all CRUD paths, batch order updates, item/attachment ownership, allocations, wallet, settlement, approval/rejection, and export with forged IDs and restricted roles. Test grants/revocations in two active browser tabs and open forms.
- Check all indirect readers and writers in Deal, Cartable, reporting, QC, warehouse, cash, request-amount and external mapping modules. Existing role checks in some application commands and cards remain additional restrictions; confirm the intended permission matrix before removing them.
- Check customer receipt/payment flows and permission boundaries. Test legacy financial totals with restricted views and related customer/deal references.
- Development config flags are present but default to false until these checks pass. Set individually to true in Development for controlled testing after the catalog exists. Do not activate in production before full review.

No database, build, browser or production verification was performed by the source-only change.
