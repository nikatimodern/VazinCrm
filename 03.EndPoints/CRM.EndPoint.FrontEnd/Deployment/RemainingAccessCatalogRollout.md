# Access catalog rollout: entities 24–47

## Status

The registration SQL and migration cover the 24 remaining entity definitions and 89 permission keys. The 347 role assignments are proposed initial grants, not a verified copy of effective production permissions. Existing grants are preserved. Review the SQL preview before applying.

Development currently enables the earlier 23 entities, SentSms, ReceivedSms, QualityControl and QualityControlQuestion (27 of 47). The other 20 flags remain disabled. Production configuration is unchanged.

## Staged work and outstanding checks

| Entity | Work | Outstanding |
|---|---|---|
| QualityControl / QualityControlQuestion | Endpoint and CQRS gates, parent scopes, shared customer state, supplier enrichment, UI and live invalidation; Development flags enabled | Non-production SQL/browser integration checks, parent grant verification, question administration if a separate workflow is requested |
| Product / ProductAmount | Lookup, price endpoint checks | Embedded selectors, price-role configuration, nested reads and UI |
| ProductGroup | Catalog registration | Group relationships and actual use cases |
| Contract | Scoped controller checks | Attachment persistence and private file delivery, views |
| Carizo | Assigned-user scope, controller/list checks | Shared task children, side effects and notifications, UI |
| Report / CallRecording | Report/participant scope, export and listen checks, dashboard count | Parent customer/deal permissions, other call routes, nested projections, background jobs and UI |
| Chat / Email | Participant and author/recipient checks, catalog gates | Attachments, hub authorization and notifications, UI and counts |
| Contact / Agent | Catalog registration | Parent customer scope, nested read/write paths |
| ReleaseNote | Administration gates | Preserve audience-based reader behavior, administration UI |
| LogSystem / SystemWarning | Dashboard and warning gates | Entity-specific log and warning source access, indirect routes and UI |
| Company / ASetting / BaseItem / BaseDescription | Selected endpoint gates | Shared configuration use, embedded forms, files and other direct/indirect paths |
| ExternalSoftware / ExternalMapping | Management and mapping endpoint gates | Transfer/export workflows, related entity checks and UI |
| SentSms / ReceivedSms | Admin-only page and API gates, separate view/sync controls, live state | Build, permission revocation and runtime tests |

## Deployment

1. Back up the UserConnection database. Preview RegisterRemainingAccessCatalog.sql with @Apply=0, validate entity namespaces and grant choices, and apply using the normal deployment procedure or the corresponding EF migration.
2. Build the entire solution and run integration tests on a non-production database. Test authorized and denied direct URLs, mixed grants, explicit denies, ownership and selected-user scopes, and role compatibility.
3. Complete remaining indirect, frontend and real-time checks before enabling each staged flag.
4. Do not interpret catalog registration as a completed authorization rollout. No production database changes or production configuration activation have been performed.

## Follow-up hardening

- Centralized owner-scope entity support across effective permissions, administration and history restoration, including the warehouse assignment resolver.
- Scoped recycle-bin queries and restore operations, including related customer, invoice and accounting checks. Report queries and writes now check related customer/deal visibility when Report enforcement is enabled.
- Added private chat/contract attachment request authorization before static file serving. Contract uploads preserve creator information and reject foreign attachment identifiers. Legacy files without valid database associations must be reconciled; other attachment directories still require individual review.
- Restricted hub authentication and removed the legacy unrestricted chat broadcast. Task/call broadcast payloads, notification recipients and real-time role group revocation still require end-to-end review.
- Scoped source-document checks for financial destination lookup and cash mapping cleanup. Other mapping and export paths require review.
- Added separate price action permissions and contract row action controls. The remaining module UI and shared/child record operations still need completion.
- AccessCatalog_CompleteSetup.sql consolidates six registration scripts (45 entities, 210 permissions, 923 proposed grants) and validates the 15 existing Customer/Deal permission prerequisites. The original Customer/Deal backfill is not present in this branch. Do not treat this as a standalone clean-database installer. Default preview is read-only; existing grants are preserved.
- Added tests/CRM.AccessCatalog.Checks and .github/workflows/access-catalog-checks.yml. On workflow run 45370240764, architecture and coding-standard checks passed; build and scope execution had not yet been confirmed at the time of this review. JavaScript syntax checks passed for accessLive.js, sms.js, chat.js and the contract inline script. These checks do not establish runtime database authorization or live revocation correctness.

At the time of the initial hardening batch, Development had 25 enabled entities out of 47. See the QC activation update below for the current status. Do not merge into main until the remaining implementation and integration checks pass. No production database operations have been performed.

## QC parent-scope hardening

All six invoice-item query paths now share a parent visibility filter when QualityControl enforcement is enabled. Customer.View (including exclusions), Deal.View and Factor.View are intersected before list aggregation, item reads, contact eligibility, review writes and note/history eligibility. The existing disabled-flag behavior is preserved. QC roles must be granted the required parent read permissions before enabling this flag; no grants are automatically widened. Shared customer-wide state and supplier enrichment, UI and SQL Server integration tests remain rollout gates. The flag is intentionally unchanged.

## QC related sources and shared state

Supplier enrichment and search use the existing document read filters for pre-invoices, supplies, production heads, supplier invoices and warehouse products. Sales invoice snapshot fields remain visible as part of the authorized sales invoice. Customer-wide notes/history and contact mutations require visibility of every eligible invoice, not merely one. Item review retains its existing per-item eligibility; automatic contact clearing additionally requires SetContactStatus and complete customer eligibility. Queue-level contact metadata remains a separate review item. SQL Server integration checks and activation remain pending.

## QC queue shared metadata

The queue now filters shared contact records, note-existence markers and contact history before querying status filters, sorting, dashboard counts, retry cooldowns and page badges. The same complete-customer eligibility predicate is reused for direct shared operations; partially visible customers retain their authorized invoice-item counts but receive no shared contact metadata. No per-customer database queries are introduced. The disabled QC flag retains legacy customer visibility. Verify against SQL Server with mixed visible/hidden invoices, all contact statuses, date filters and direct history requests before activation. Suppressed contact status is represented by the existing neutral value, not a claim that the customer was contacted.

## QC UI permission wiring

Queue responses now include the same complete-customer capability used by shared contact operations. Razor exposes current catalog grants to the QC page; JavaScript hides unauthorized controls, prevents stale writes and invalidates pending reads after live access changes. History management additionally requires the exact catalog grant, not just the admin role. Question viewing is required for opening the review form and saving reviews. Browser and SQL integration validation remain prerequisites for enabling the QC flags.

### QC activation acceptance checks

- Apply the catalog registration on a non-production UserContextDb and verify the QC role has the required parent read permissions; do not automatically grant broad Customer/Deal/Factor access.
- On a non-production CRM SQL Server, verify a customer with mixed visible/hidden invoices: item list and counts include only visible invoices; contact/notes/history metadata and shared operations are denied; date filters cannot expand the scope.
- Verify selected and excluded parent owners, foreign item/note/history IDs, deleted rows, contact updates, and supplier enrichment (direct and warehouse allocation).
- Verify separate View/Create/Edit/Delete/SetContactStatus/ManageContactHistory/QualityControlQuestion.View grants, including admin without ManageContactHistory. Verify view-only buttons, note editor, pending bulk review, revoked permissions during open dialogs and failed state refresh.
- Review whether there is an existing question administration entry point beyond the seeded questions; no new question CRUD workflow is introduced by this permission change.
- Confirm CI and real browser/SQL integration tests before enabling both flags. JS syntax is checked in CI; syntax alone does not establish authorization.

## QC Development activation and question review

The user requested activation of both QC flags in Development. QualityControlQuestionConfig seeds three questions and excludes soft-deleted definitions; GetQuestionsAsync returns active definitions only. GetCustomerForm and SaveItems require QualityControlQuestion.View at both the endpoint and CQRS boundary; SaveItems validates the entire active question set, unique question IDs, binary answers and at least one discrepancy, and stores historical answer snapshots. No dedicated question administration controller/action was identified in the reviewed QC, settings, admin and base-item controllers; the catalog Create/Edit/Delete definitions do not create a question CRUD workflow. The question definitions are shared configuration, so arbitrary owner scopes are not supported. The page-level contact grant attribute has been renamed to avoid colliding with delegated contact action selectors.

Both QC flags are now true in appsettings.Development.json only. The previous CI run 45380408930 succeeded; the new activation commit requires its own CI result. Parent Customer.View, Deal.View and Factor.View permissions must be explicitly verified for each QC user in the test UserContextDb; no grants have been widened, and missing grants deny access. No SQL Server or browser runtime verification was performed here. Development activation is not approval for production deployment or merge. Complete the acceptance checks above before production activation.

## Product price first hardening pass

Price query and write CQRS handlers now check catalog grants independently; writes keep the existing price-editor role set. Price edits whitelist business fields, reject product reassignment and invalid ranges, and require an active parent product. Price deletion is POST with antiforgery, as is price saving; the amount page uses the shared request helper without automatic mutation retries. The product lookup requires authentication and excludes deleted products. Product-group and embedded product/price consumers, role grant alignment and full browser/SQL checks remain open. No new flags were enabled.

## Per-entity acceptance standard (24–28)

Each entity requires catalog registration, all applicable permissions, legacy-user access mapping through role membership without overriding explicit user grants, direct and indirect backend/frontend enforcement, and separate preview-first SQL scripts. RegisterQualityControlAccessCatalog.sql and RegisterProductAccessCatalog.sql are scoped to the five entities and list affected users through AspNetUserRoles in preview. The price editor role grants now include Create/Edit/Delete for the three legacy editor roles; existing role grants and user overrides are not overwritten. BaseItem product listing/creation UI follows Product.View/Create; the unused delete button was removed rather than introducing a new delete operation. ProductGroup has no verified independent CRUD workflow yet. QC has endpoint/CQRS and UI checks, but existing parent grants and runtime behavior must still be verified. No production SQL has been executed; no additional enforcement flags were enabled.

## Product dependent selectors and price modal

The shared catalog selector and price modal discard cached catalog data and block further lookups after access invalidation. The shared modal is rendered only with both Product.View and ProductAmount.View. The product lookup can filter by group only with ProductGroup.View; an authenticated group lookup returns IDs and names with ProductGroup.View. PreFactor saves check new/changed catalog links and preserve existing snapshots, and batch-resolve catalog rows instead of per-item queries. Existing group CRUD administration was not identified; this change does not introduce group mutation actions. The five registration SQL scripts were reported applied by the user (two scoped scripts for five entities); actual effective UserContextDb grants and browser/SQL behavior require verification. No additional grants or flags are changed here.

## Product group visibility and grant verification

The existing product settings table now includes the canonical group label only when ProductGroup.View is granted; its placeholder operations column is removed. The new read-only VerifyQualityControlProductAccessCatalog.sql checks catalog activity and effective configured user/role scopes including explicit overrides, and highlights QC users missing dependent grants. Selected/excluded owner membership and real SQL/browser behavior still need runtime tests. No registration changes or additional writes are needed for this verification. The per-entity acceptance standard is recorded in AGENTS.md. Group administration is not an existing verified workflow and has not been invented in this authorization task.
