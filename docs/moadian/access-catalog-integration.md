# Taxation access catalog integration

## Current access boundary

The Taxation Razor and API routes require the existing `AppRoles.Admin` role. The menu applies the same role and Development configuration gate. The Sandbox transport additionally requires an enabled, allowlisted HTTPS endpoint. Mutating API methods require ASP.NET Core antiforgery, and sensitive responses use `Cache-Control: no-store`. Admin access in this interim configuration covers all Taxation records.

The finance and ERP approval flows are independent of this gate. No Taxation permission entities, roles or grants are created by this branch.

## Reviewed dependency

The GitHub `AccLevel` tip reviewed was `d5f54c5c4a5786ac9a36f3aaa7b88d23619901a1`. It is outside the `moadian` history and supplies `CatalogPermissionAttribute`, `DocumentAccessPolicy`, `IDocumentReadAccess` and the access catalog migrations in `UserContextDb`. Taxation has no entity in that revision's enforced catalog. Final integration requires the approved upstream revision on the intended integration base.

## Apply the existing catalog and scopes

Register Taxation operations using the approved catalog conventions when the dependency is available. The labels below describe operation groups. Assign exact catalog keys during integration.

| Operations | Scoped resource |
| --- | --- |
| Module navigation and settings visibility | Taxation module and existing settings authorization |
| Invoice listing, preview, Snapshot, dispatch detail | ERP `Factor` through `IDocumentReadAccess.FactorsAsync` |
| Queue, manual submit, processing inquiry and work-folder inquiry | The linked ERP factor, plus the catalog action for each operation |
| Batch queue | Every selected ERP factor independently |
| Referenced documents | Both the original and candidate ERP factors when present |
| Tax payment registration, submission and status | Linked factor and accounting receipt, with the relevant accounting scope |
| Report, counters and Excel | The same factor scope before counting, paging or exporting |
| Fiscal memories, product mappings and Sandbox settings | Catalog gates for global configuration and scoped product access where applicable |

Apply view and operation permission checks in the backend before reading sensitive data or preparing external requests. Apply document filtering to the SQL queries before projecting, grouping or exporting. Resolve Snapshot, dispatch and payment IDs through their persisted ownership links. Keep CSRF and HTTPS configuration gates independent of catalog grants. Add deny cases for mismatched IDs, another owner's records, aggregates and export, and permission changes between listing and mutation.

## Audit records

The Taxation MVC/API action filter records event `6201` through the application's structured logging sink. Fields are controller, action, authenticated subject ID, an optional routed internal GUID and the HTTP action outcome. Invoice and payment state history is also stored by the corresponding domain workflows. The current application's rolling file sink retains up to 30 files. Deployment owners can forward these metadata-only events to the approved audit sink if longer retention is required.

Do not enable request or response body logging, EF sensitive data logging or HTTP header dumps for Taxation. Keep login and authorization failure monitoring in the platform pipeline. The browser Taxation callers suppress shared API exception dumps while displaying the normal operation feedback.

Mock transport is registered by the isolated test host and writes to a dedicated disposable LocalDB database. The application transport accepts the configured Sandbox origin only, with redirects disabled. Taxation routes are unavailable in Production.
