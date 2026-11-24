# Transfer and cash access rollout (entities 14–19)
Branch: AccLevel. Catalog database: UserConnection / UserContextDb only.

Registration: `RegisterTransferCashAccessCatalog.sql` (preview by default, @Apply=0) or migration `20261125000200_RegisterTransferCashAccess`. Six entities, 31 permissions, 96 proposed initial role grants. Review actual role assignments before applying. Existing grants/overrides are preserved. Retain separately for eventual AccessCatalog_CompleteSetup.sql.

| Entity | Keys | Owner |
| --- | --- | --- |
| Transfer | View, Create, Edit, Delete, ChangeStatus, Approve | creator |
| Cashier | View, Create, Edit, Delete | assigned UserId |
| Cash | View, Create, Edit, Delete, ChangeStatus, Approve, ExportExternal | creator |
| BaseCash | View, Create, Edit, Delete | creator |
| BaseCenterCash | View, Create, Edit, Delete | creator |
| CashGroup | View, Create, Edit, Delete, Approve, Export | creator |

Create is unscoped. Other keys support Own, All, SelectedUsers and AllExceptSelectedUsers. BaseCenterCash has existing read/selection paths, not a standalone CRUD UI; its remaining CRUD keys are reserved. CashGroup creation, approval/rejection and export use existing workflows; its independent Edit/Delete keys are reserved rather than introducing unsupported standalone mutations.

## Implemented
- Document policy, endpoint guards, read queries and counts, per-row action rights, main Razor controls, JS guards, navigation and AccessLive integration.
- Transfer: bank references (old/new), creator/status preservation, status approval, receipt attachment ownership.
- Cash: old/new cost title, cost center, cashier and order references, bank account scope, attachment ownership and protected status; approval, batch submission and external export.
- Cashier: view/edit/delete and scoped user options, linked bank checks, balance data limited to complete authorized ledger totals.
- CashGroup: scoped group totals and member checks for group operations; no aggregate for groups containing inaccessible cash rows.
- Earlier rollout follow-up: cashier target scope on RequestAmount lists/details and payment operations; old bank references and protected ownership on legacy supply/payment edits; shared attachment checks for transfer/cash.
- Development flags for all nineteen implemented entities are true. Production configuration unchanged.

## Before use
- Back up isolated test UserConnection. Review and apply all prior catalog registrations and this registration (SQL @Apply=1 or corresponding EF migration, not both as separate deployments).
- Review actual finance/manager/cashier role membership, particularly self-owned cashier access and the complete-ledger balance behavior.
- Build the solution, including Razor. Check JavaScript/runtime, all four scope modes, explicit denies, own/all/selected/excluded owners, forged IDs and parent references, status transitions, batch approval/export with mixed allowed/denied rows, stale/open forms and SignalR revocation.
- Verify indirect external mapping, reporting, warehouse, cartable and accounting integrations and authorization of nested data when their rollouts proceed. Review existing role attributes versus custom roles before removing old restrictions. Verify any standalone expense-center operations if added later.
- No database migrations, SQL apply, .NET build or browser regression tests executed in source-editing environment. JavaScript syntax checked for modified standalone scripts and inline scripts with Razor syntax substituted.
