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

## Product catalog Development activation

Reviewed direct product/group/price endpoints, BaseItem listing and creation, shared price modal, product selection, pre-invoice catalog references, menu gates, and warehouse manual receipt/new and changed catalog references. Existing order, supply, sales invoice and warehouse document snapshots remain under the parent document permissions; copied historical transaction information is not treated as access to the live catalog or price list. Existing group administration CRUD was not found in the reviewed paths; no new mutation workflow is introduced. Both scoped registration scripts were reported executed by the user. Activate Product, ProductGroup and ProductAmount in Development only at the user's request (30/47 flags). Production settings and main remain unchanged. Actual effective grants, SQL translation, browser integration and the activation commit's CI must still be verified; this is not a production release approval.

## Mandatory entity handoff sequence

Follow the ordered access catalog acceptance workflow in AGENTS.md. Finish implementation and pre-handoff checks before delivering the final SQL script. Do not move on to another entity until the user confirms successful script execution and any database-dependent acceptance checks are resolved or explicitly accepted as limitations. The user reported the two scoped registration scripts for entities 24–28 executed; the subsequent Development activation build for ee280b31 and preceding builds for 526c1572 and 0150a0b0 are successful. Runtime SQL/browser and actual effective user grant verification remain separate unverified checks.

## Contract (entity 29) implementation review

Contract has View/Create/Edit/Delete catalog entries; owner scopes apply to view/edit/delete via creator ID. The existing endpoint policy is NotCustomerOnly (any authenticated user without the Customer role); staged grants are Admin only and the reviewed menu does not contain a Contract entry. Resolve this discrepancy before final grant SQL/activation; do not silently grant contract write access to all non-customer roles. Hardened save/delete POST antiforgery, active record and attachment checks, upload field binding and allowed extensions, read-only UI and stale request suppression. Legacy shared ContractAtt directory includes non-contract files and existing file associations require verification. Contract enforcement flag remains unchanged. No final registration script is handed off while grant mapping and integration checks remain open.

## Contract SQL handoff

Contract implementation builds ebb7b0a8 and e48f91ec succeeded. RegisterContractAccessCatalog.sql is the scoped, preview-first handoff for entity 29 and uses the same four Contract permission keys and Admin-only initial grants already in AccessCatalog_CompleteSetup.sql and RegisterRemainingAccessCatalog.sql. No consolidated grant changes are needed because the entries match. The scoped script reports existing grants/overrides and does not overwrite them. The old NotCustomerOnly endpoint was broader than the staged role list; no approved additional role grant was inferred. Users without existing or individually approved Contract grants may lose access when the flag is enabled. Runtime SQL/browser tests, legacy file association checks and user confirmation of script application remain outstanding. Contract enforcement remains disabled until handoff confirmation.

## Contract Development activation

The user confirmed RegisterContractAccessCatalog.sql was executed. Contract enforcement is enabled in Development only (31/47 flags). Existing explicit overrides remain intact. Actual SQL grant results, legacy attachment association checks and runtime browser/SQL authorization tests were not independently observed. The staged Admin initial grants are unchanged; additional users require approved grants. Production configuration and main are unchanged.

## Entities 30–35: Carizo first hardening batch

Task repository now exposes per-row and shared mutation scope checks and active task queries. Save/delete and direct task reads apply additional CQRS guards, prevent ownership/system/status overposting and reject foreign participant IDs. Shared edit/delete conservatively require scope over every affected row; assigned-user status propagation, manual linked tasks, notifications, UI and all runtime compatibility checks are still outstanding. No flags or final registration SQL are delivered in this incomplete batch.

## Report / CallRecording follow-up

Report CQRS update/delete now intersects visible report and action scopes; report edits preserve AI/call linkage and prevent graph overposting under enforcement. Report cards check action scope and the audio endpoint's Listen grant. Existing linked manual Deal/Report task creation remains supported at the controller with parent visibility checks; unrecognized linked types are denied. Remaining issues include direct background report commands and call event notifications, participant display, legacy role mapping, shared form/live UI and runtime integration. No new flags enabled.

## Chat / Email follow-up

Chat mutations and email detail/mark-read POST now use antiforgery. Chat UI uses action-specific grants and reloads on permission invalidation; attachment uploads accept only fresh whitelisted files and deletion is soft. Recipient Chat.View is checked before chat push and reminder delivery. Email returns actual seen state and retains sender/recipient isolation. Shared counters, queued notifications, other callers, browser/runtime compatibility and linked records remain to review. Contact is a separate entity with Customer/Agent type and EntityId; no standalone Contact CRUD was identified in the reviewed files. No flags enabled and no final SQL handoff yet.

## Carizo status and effects

All seven remaining manual command handlers recheck action scope. Shared completion/uncompletion validates the persisted root and current actor membership before modifying group rows; normal assigned-user group side effects are preserved, while arbitrary group mutation is denied. Refresh broadcasts no longer include task IDs; notification content is generic under enforcement, and legacy push details are suppressed until queued-recipient authorization is implemented. Actual browser behavior and shared-task lifecycle tests remain pending.

## Contact scope and notification review

Contact is a distinct Customer/Agent-linked model. The owner resolver supports Contact and intersects contact creator scope with active parent customer visibility, including agent-owned contacts. Existing customer creation strips Contacts and update maps scalar values only; no independent Contact endpoint/UI has been verified. Carizo visibility intersects known linked Deal/Report/Accountancy access; timed reminders and deferred pushes avoid exposing cross-document text. Asterisk call notifications target the destination user rather than broadcasting call IDs, and under CallRecording enforcement check that recipient's view scope before displaying an incoming call. Queue-level delivery and call lifecycle correlation still require integration tests. No flags or final SQL delivered in this batch.

## Carizo and Report shared UI/mutation paths

Shared task and report mutation paths now send antiforgery tokens and use POST/DELETE; report automatic mutation retries removed. Task cards use scoped edit/delete/status capabilities, shared JS guards, and report create/export buttons use exact catalog grants. Report central visibility intersects CallRecording.View when enabled. Full browser and SQL tests and final grant mapping remain open; no final SQL handoff or activation in this batch.

## Entities 30–35 grouped SQL and verification handoff

The six-entity batch is registered in RegisterCommunicationAccessCatalog.sql (6 entities, 22 permissions, 214 proposed role grants); the two consolidated registration scripts have matching rows. The script is preview-first, idempotent and preserves existing grants and explicit overrides. Legacy ReportUserContext grants other-report edits to Admin/SalesAdmin and manager playback to Admin/SalesAdmin/ManagingDirector/ExecutiveDirector; corresponding proposed manager report/call scope defaults now match those roles. Existing role scopes that differ are reported but not overwritten. Other role defaults remain the original conservative Own staging, so formerly broad non-manager report reads may be narrowed; no new broad Contact grants are inferred. Existing historical migration is not rewritten. Contact has parent-customer and creator scope in the central policy but no independently verified management page; no new CRUD workflow was invented. Shared Carizo edit/delete requires visibility and action scope across all affected rows. Own status propagation remains the explicit legacy coordinated group operation for persisted participants; non-Own scopes must authorize all affected rows. Chat and Email counters and header navigation, task/reminder controls, call notification buttons, live invalidation and queue delivery checks were hardened. Push content is generic while enforced. SQL/browser and effective database grant verification remain external checks, including parent permissions, shared task lifecycle and call correlation. No new flags are enabled, no production writes or main merge. Review the preview and only apply after the relevant CI build passes.

## Entities 30–35 Development activation

The user confirmed that RegisterCommunicationAccessCatalog.sql was applied successfully (including a second sequential execution). The reviewed script inserts only missing entities, permissions and role grants; existing scopes and explicit overrides remain unchanged. Enable Carizo, Report, CallRecording, Chat, Email and Contact in appsettings.Development.json only (37/47 flags). The preceding CI run 45400430818 passed the solution/Razor build, project-reference architecture checker, coding standards structure, JavaScript syntax and access-scope checks. This activation requires its own CI result. The actual UserConnection effective grants, parent-document scope, browser/live revocation, shared task lifecycle, private attachments and call correlation have not been independently verified against a non-production database. Obtain runtime verification or explicit acceptance of the outstanding external validation before advancing to Agent. Production configuration and main are unchanged.

## Dynamic role decoupling regression fix (first batch)

The reported QC grant was blocked by legacy role filtering in the menu, controller and application policy. Catalog-enforced QC now uses exact effective grants; contact history administration also uses its exact grant rather than the legacy administrator name. The parent Customer/Deal/Factor visibility intersection is unchanged. Disabled flags keep legacy roles. The menu now resolves the listed catalog View links before legacy roles, including nested groups; employee-only endpoint boundaries remain. Legacy endpoint role filters are conditional for the inspected Amount, RequestAmount, SupplyFactor/Payment landing pages, BaseItem/BaseCash landing pages, selected Cash/Cashier/BaseCash actions and BankAccount writes. This is a partial rollout: application/service and Razor role gates in other modules, financial exports, mixed-flag actions, SMS, Cartable and account-type boundaries need separate review. No SQL, grants or enforcement flags changed. Run CI and test QC with a non-QC role, explicit allow/deny, question and parent grants, and disabled-flag legacy behavior.

The follow-up preserves inherited role restrictions for non-catalog menu links even when a catalog-granted sibling opens the parent group. Product price CQRS commands and list capabilities now use the individual Create/Edit/Delete grants when enforced, retaining legacy editor roles only while disabled. QC history management Razor capability follows the same flag-aware rule as the CQRS handler. No changes to grants or SQL.

## Dynamic permissions follow-up: order and report commands

Order head update, delete, change status and bulk save now evaluate the exact operation and View against the repository's scoped query; the legacy role/creator gate is used only if OrderHead enforcement is disabled. Creation checks its catalog grant. Report update and delete no longer reject a permitted delegated scope based on the old edit-others role capability. Report header Create uses the effective grant when enabled. Reviewed cash-group approval/rejection and request-payment endpoints use the existing exact checks, retaining legacy role fallback whenever either governing flag is disabled. This is not a claim of complete project-wide migration; remaining role gates, especially indirect financial paths and embedded controls, require review. No SQL or production flags changed.

SMS sent/received view and sync endpoints now use their separate effective grants instead of an Admin-only controller gate when enforced; combined dashboard still requires both. Shared settings modal and navigation expose delegated SMS and financial base-data links. The legacy fallback remains per entity when disabled. AGENTS.md now makes role-independent authorization mandatory and CI runs a read-only project-wide candidate inventory. The inventory is not proof of semantic correctness; remaining financial-export, embedded controls, cartable, integrations, realtime and role-dependent service paths are not yet accepted. No new grants or flags enabled.

## Entity 48: Dashboard (implementation batch)

Dashboard is a virtual catalog module, not a CRM table. New independent View and widget gates, dashboard owner scope intersected with source document scope for sales/details/order/customer counts/report counts, and inherited source scope for shared report/task lists. Full financial totals require all four source grants and expose only permitted result fields; raw breakdowns are suppressed under Dashboard enforcement. Audit history is withheld until LogSystem enforcement is available. Dashboard-specific report and task reads, legacy aggregate route and user lookup are gated; the latter no longer enumerates all sales-role members under enforcement. Navigation, Razor cards and live access state use the new flag. The denied dashboard renders a safe shell rather than signing the user out. Dashboard remains disabled pending scoped SQL handoff, CI and runtime checks. No existing grants changed; no production or main changes.

Dashboard registration is supplied by RegisterDashboardAccessCatalog.sql (13 permissions, 83 proposed grants) with matching consolidated rows. The five scoped aggregate keys use seller/creator ownership; ViewReports and ViewTasks inherit their source scopes instead of adding a second owner selector to the shared CQRS lists. Legacy navigation roles receive the initial grants; four legacy full-aggregate roles get All, others Own. Only Admin is initially offered audit-log grants, and the widget stays unavailable until LogSystem enforcement is enabled and verified. Source flags are required when Dashboard enforcement is on. Financial breakdowns are intentionally hidden to prevent disclosure of independently denied metrics. No new CRM table. Dashboard remains disabled until SQL confirmation and acceptance checks. Runtime browser and SQL Server integration verification is not available in this connector session; test direct endpoints, denied widgets, owner selections/exclusions, live changes and role-independent access before activating. No main merge or production changes.

## Entity 48 Development activation

The user reported assigning Dashboard permissions to a user and explicitly requested activation in response to the SQL confirmation prompt. Treat this as the user's authorization to enable Development with the catalog present; complete SQL execution and effective grants have not been independently verified. Final implementation CI run 45413256016 succeeded. Enable only EnforceDashboardPermissions in appsettings.Development.json (38/48 planned modules). SQL Server and browser acceptance checks remain unverified: test Dashboard.View, Dashboard.ViewSales, Factor.View, intersecting scopes, explicit denies and live revocation with the affected user. LogSystem remains disabled and Dashboard logs remain unavailable under enforcement. Production and main are unchanged. Activation does not constitute complete runtime acceptance.

## Entities 36–39 administration and audit batch (staged)

ReleaseNote administration uses exact grants in controller and CQRS/service; the published audience and acknowledgment workflows remain independent. Settings/menu and action visibility are permission-aware. Agent owner resolver supports creator scopes and requires visible parent Customer; Agent contacts intersect Agent.View, Customer.View and Contact scope. No standalone Agent CRUD routes were located in the inspected code; do not invent management UI. LogSystem enforces its own View/ViewDetails and source record/parent permissions on dashboard and embedded history. Unmapped/security administration logs (AppUser), deleted/unresolvable records and unknown parents are withheld pending verified access policy for those types; dashboard filters the latest 100 candidates, so users may receive fewer than 100 visible entries. Embedded stored-procedure output must be verified against the actual SQL Server; no claim of runtime verification. SystemWarning View/Delete/Refresh and ViewQualityControl are explicit; current two warning types refer to Factor, scoped by visible Factor. Refresh modifies data, uses POST/antiforgery and requires unrestricted Factor.View under enforcement. Delete uses POST/antiforgery and checks visible warning. Scheduled warning creation and release readers remain independent of interactive management permissions. Existing styles unchanged. Concurrent dashboard fixes preserved. Initial grants: admin receives all staged keys; old internal dashboard roles receive LogSystem.View only (source scope still applies), old finance warning roles receive View/Delete/Refresh. Admin-only Agent grants may narrow historical delegated agent-contact reads; review preview and individual grants. No production/main changes. RegisterAdministrationAuditAccessCatalog.sql stages 4 entities, 16 permissions, 30 proposed role grants and matches consolidated setup. Flags remain disabled. CI and runtime checks, especially stored procedure, live invalidation, parent scopes and Agent entry-point inventory, remain open; do not activate before SQL and acceptance confirmation.

## Follow-up 36–45 (all flags already enabled by d14950421)

CI for the earlier 36–39 batch succeeded. A later independent commit enabled all ten flags before completion of 40–45; preserve existing configuration and do not treat activation as acceptance. This follow-up removes fixed role gates from the inspected ASetting and standalone ExternalSoftware administrative paths, while retaining the independent Admin gate on the mixed ExternalMapping workspace until cross-entity/user-mapping authorization is reviewed. Adds POST/antiforgery and singleton record identity checks for settings/company writes, prevents arbitrary BaseItem IDs and automatic mutation retries, gates existing action buttons and settings menu. The shared authenticated operational settings projection excludes administrative row ID and SMS provider; the settings management endpoint remains separately permission-gated. No CSS/layout changes. Company initialization when no existing singleton is present is intentionally rejected pending explicit creation workflow review; BaseItem delete buttons are legacy no-ops; BaseDescription currently has read endpoints only. Company file upload validation, all indirect mapping and document export paths, and database/browser acceptance remain pending. Existing flags and SQL keys unchanged. No production or main changes.
