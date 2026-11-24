# Warehouse access rollout (entities 20–23)

Branch: AccLevel. Catalog database: UserConnection / UserContextDb only.

Registration: `RegisterWarehouseAccessCatalog.sql` (preview by default, @Apply=0) or migration `20261125000300_RegisterWarehouseAccess`. Four entities, 19 permissions and 158 proposed role grants. Existing grants and overrides are not overwritten. Review roles and actual WarehouseUsers membership before applying. Keep SQL separate until the final AccessCatalog_CompleteSetup.sql.

| Entity | Permissions |
| --- | --- |
| Warehouse | View, Create, Edit, Delete, ManageUsers |
| WarehouseProduct | View, Create, Edit, Delete |
| WarehouseTransaction | View, Create, Edit, Delete |
| WarehouseVoucher | View, Create, Edit, Delete, Approve, Deliver |

Scope: the warehouse creator and assigned employees (WarehouseUsers) are the shared ownership set for all four entities. An explicit excluded owner wins over inclusion; scope None denies. View of the parent warehouse is required in addition to child permission; linked voucher items must belong to the same authorized warehouse. Create is unscoped as a catalog key, but child creation requires the destination warehouse and relevant child View rights. Creating a new product via receiving stock requires WarehouseProduct.Create and WarehouseTransaction.Create. Inventory stock is the full physical quantity minus reservations, not a partial sum of visible transactions; WarehouseProduct.View grants that aggregate, whereas transaction history is separately authorized.

## Implemented
- Central warehouse scope and parent-child checks; lists, counts, selected IDs, detail routes and voucher actions; member management requires ManageUsers. Main warehouse restrictions and existing manual/automatic allocation rules remain.
- WarehouseProduct updates preserve owner and immutable warehouse references; navigation graphs cannot be supplied as arbitrary update payloads.
- Voucher create/edit validates all members, prevents duplicate products and cross-warehouse moves; tracked junction rows are updated without replacing their composite keys. Delivery requires transaction creation rights and authorized warehouse scope, and validates available stock inside a serializable transaction.
- Manual edits/deletes of automatic allocation and voucher transactions are blocked. Manual stock writes cannot be used to attach arbitrary transactions to a voucher.
- Allocation follow-up: previous and proposed warehouse destinations/stock sources and linked sales/supply records are checked. Allocation stock-source searches and related allocation rows are restricted by warehouse permissions. Automatic ledger synchronization remains a system effect of authorized supply/factor workflows, not an unrestricted manual warehouse API.
- Main Razor controls, action-state data, JavaScript handlers, navigation and live access state updated. Four Development flags enabled; all 23 implemented entity flags are true. No Production flag changes.

## Verification still required
- Back up isolated UserConnection, preview and apply catalog registration (or the corresponding migration). Verify 4 entities, 19 permission keys, and approved initial role grants. Review members with custom roles and legacy manager checks.
- Build solution and Razor; run SQL Server integration and browser tests. Validate all four scopes, explicit denies and parent-child combinations, forbidden IDs, assignment changes, main warehouse rules, mixed batch products, stock reservations, concurrent approval/delivery, repeated delivery, duplicate members, authorized automatic allocation workflows, and live revocation.
- Further audit indirect dashboard/reporting/export/external mapping paths and related modules at their rollouts; recheck system allocation synchronization under real data. Do not treat source changes as an exhaustive runtime verification.
- JavaScript syntax checked. No SQL execution, .NET build, or production database changes performed in the source-editing environment.
