# Vazin CRM — Codex Project Instructions

## Scope and language
- These instructions apply to this repository. Inspect relevant code before making changes; do not assume documentation is always current.
- Communicate with the user in Persian. Keep identifiers, code comments, XML documentation, and technical file content in English. Preserve Persian UI text and business terminology.
- Prefer efficient, readable, maintainable and well-factored code. Improve relevant touched code when it reduces duplication, complexity or avoidable overhead. For broader refactoring beyond the requested scope, first present a concrete plan (affected files, benefits, risks, verification) and obtain explicit user approval before editing. Do not introduce packages, layers, or frameworks without a concrete need and approval.

## Onion architecture and dependency boundaries
This is a layered Onion Architecture project. Follow these responsibilities even where legacy code has not yet been migrated:

- `CRM.Framework`: shared, domain-independent framework utilities and cross-cutting primitives. Keep it independent of business modules and infrastructure.
- `CRM.Core.Domain`: domain entities, value objects, enums, business invariants, domain-level behavior, and **repository interface contracts**. Domain must not depend on SQL, EF Core, MVC, or other outer layers.
- `CRM.Core.ApplicationServices`: existing application layer for CQRS commands, queries, handlers, and application orchestration. For each new feature, assess whether CQRS is appropriate and propose its adoption when useful. Use repository interfaces defined in Domain. Inspect the existing project structure, packages, handler patterns, and DI setup before implementation; do not assume a particular CQRS library is installed.
- `CRM.Infrastructures.Sql`: database access, `DbContext` implementations, EF Core mappings/Fluent API, migrations, and **implementations of repository interfaces declared in Domain**.
- `CRM.EndPoint.FrontEnd`: ASP.NET Core MVC application, Razor views, controllers, HTTP endpoints, frontend assets, and composition root/DI registration. Controllers should remain thin and delegate application/business work to the appropriate layer.

Dependency direction: outer layers may depend on inner layers, never the reverse. Domain must be independent; ApplicationService depends on Domain contracts; SQL infrastructure implements Domain contracts; FrontEnd composes and calls the layers. Keep framework utilities dependency-safe.

For new features, prefer the intended architecture. Do not add business rules to controllers or EF mapping classes. For every new feature requiring persistence, define the repository contract in Domain and implement it in SQL Infrastructure; do not bypass this boundary by accessing DbContext directly from a new controller or CQRS handler. Avoid EF Core dependencies in Domain and CQRS handlers. For existing legacy features, make scoped changes without undertaking an unrequested architectural rewrite.

## Repository map
- `01.Core/CRM.Core.Domain`: domain entities, business contracts, and repository interfaces.
- `CRM.Core.ApplicationServices`: existing application layer for CQRS; inspect its actual folder location, project references, dependencies, and DI registrations.
- `CRM.Framework`: framework/shared layer; inspect its current path and contents.
- `02.Infrastructures/CRM.Infrastructures.Sql`: SQL persistence, EF Core contexts, repository implementations, and Fluent API.
- `03.EndPoints/CRM.EndPoint.FrontEnd`: MVC endpoints, Razor, frontend scripts and styles, and DI composition.
- Main CRM database context: `CRMContextDb`. Sepidar integration currently uses `SepidarContextDbBase`, `Sepidar1Service`, and `Sepidar2Service`; inspect actual registrations and types before changing integrations.

## Backend conventions
- Follow the Onion Architecture boundaries above. For every new feature requiring persistence, repository interfaces are mandatory in Domain, with their implementations in Infrastructure. Do not create a repository for a feature that has no persistence need.
- For new features, assess and suggest CQRS when it fits; propose CQRS when beneficial and obtain approval before introducing a new CQRS pattern or dependency. Place approved commands, queries, and handlers in the existing `CRM.Core.ApplicationServices` project, following its actual structure.
- Reuse existing patterns when maintaining legacy code, but do not spread architectural violations to new code.
- Use async I/O and `CancellationToken` when appropriate.
- Respect ASP.NET Core Identity, `[Authorize]` roles, server-side validation, and HTTP response contracts.
- Distinguish page navigation from AJAX/API responses; preserve cookie-expiry and error-handling behavior.

## CQRS organization: Feature-Based / Vertical Slice
- Use this structure by default for every new Command and Query across all features, not only Reports. Place each feature directly under `CRM.Core.ApplicationServices` without a `Features` wrapper folder.
- Give each operation its own folder: `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/`. Keep its request, handler, and operation-specific DTO/result together. Do not create a feature-wide `Handlers` folder. Use `<Feature>/Dtos/` only for models actually shared by multiple operations.
- Match namespaces to the operation folders. Name requests, handlers, and results according to actual behavior; for example, use `GetAllReportsQuery` for retrieving all reports and `GetReportQuery` for retrieving one report. Do not change behavior merely to match an example name.
- Keep generic CQRS contracts in `Abstractions/Cqrs`. Repository interfaces remain in Domain and implementations in SQL Infrastructure; handlers depend on Domain contracts, never directly on DbContext.
- Reuse the current CQRS contracts and DI approach. When moving files, update namespaces, usings, DI registrations, and callers while preserving HTTP responses, authorization, and business behavior. Add no unnecessary files or abstractions, and obtain approval before adding packages.

## EF Core and SQL Server
- Inspect entity configuration and actual schema before assuming column or relationship names.
- Prefer server-translatable LINQ, focused projection, and `AsNoTracking()` for read-only operations. Avoid N+1 queries and unnecessary materialization.
- Do not call arbitrary C# normalization/parsing methods inside EF queries unless translation is verified.
- Never execute destructive SQL, database migrations, schema updates, or writes to production/external databases without explicit approval. Migration generation also requires explicit request.
- For cross-database operations, do not assume a single transaction covers CRM and Sepidar; identify partial-failure and retry risks.

## Frontend conventions
- Inspect related Razor, JavaScript, CSS, and shared utilities before making changes.
- Prefer TomSelect for enhanced selects; do not introduce Select2 in new code. Consider migrating touched Select2 controls when practical and within scope.
- Prefer vanilla JavaScript and native DOM APIs. Use jQuery only where genuinely required by existing integrations, such as DataTables.
- Inspect `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/site.js` and reuse existing public helpers such as `sendApiRequest`, Toastify notifications, loading, validation, and formatting functions when appropriate. Verify actual function names and signatures first.
- Discover and reuse existing JavaScript classes/components when they fit the task; avoid forcing abstractions into simple implementations.
- Prefer scoped, purpose-built CSS and project design conventions. Bootstrap interactive components (e.g., modals) and useful utility classes are welcome, but avoid relying on Bootstrap's default visual component styling for bespoke interfaces.
- Preserve RTL and Persian UI; handle TomSelect/modal scrolling, focus, initialization and cleanup.
- Prevent duplicate event listeners, repeat submissions, unnecessary DOM work, and stale state.
- Write efficient, clean, maintainable code. Refactor touched code when it reduces duplication or complexity. For broader changes, propose a plan and wait for explicit approval.

## Financial and Sepidar safety
- Treat invoice, discount, tax, cash, and accounting calculations as high-impact. Use decimal-compatible arithmetic and preserve established formulas unless changes are requested and validated.
- Before exporting records, inspect `ExternalMapping`, existing deduplication checks, required mappings, and external ID types.
- Select the correct Sepidar service/context for the target external software. Never guess table columns, fiscal year, account codes, or foreign keys.
- Do not log secrets, connection strings, credentials, personal data, or full financial payloads.

## Workflow and verification
1. Inspect the smallest relevant set of files and any local instructions.
2. State assumptions when requirements or existing behavior are ambiguous.
3. For new features, identify mandatory Domain repository contracts and their Infrastructure implementations when persistence is involved; propose CQRS when useful and seek approval before introducing it.
4. Make scoped edits; preserve public contracts unless requested. For broader refactoring, present an impact/risk/test plan and wait for approval.
5. Run the narrowest practical build/test/lint checks when available; report commands and outcomes truthfully. Never claim tests passed if not run.
6. Summarize changed files, behavior, risks, and any manual checks needed in Persian.
- Do not commit, push, merge, create branches, or open PRs unless explicitly asked.

## Skills
### UI design standard

- When working on access permissions or backend logic, preserve existing UI design, CSS layout, sizing, spacing, colors and responsive behavior. Hide unauthorized controls or sections without redesigning the remaining interface. Any visual redesign requires explicit user approval.
When creating or editing UI (including design previews), load and follow [.agents/skills/project-ui-design-system/SKILL.md](.agents/skills/project-ui-design-system/SKILL.md) and its design-token reference for the official palette and component styling. Apply it alongside the frontend skills below; this does not authorize global restyling or changes outside the requested scope.

Use relevant skills in `.agents/skills/` when the task matches their descriptions. The repository's actual code and explicit user instructions take precedence over examples in skills.
For every frontend task in any module (including Razor, JavaScript, CSS, and frontend components), load `.agents/skills/crm-frontend-reusables/SKILL.md`. Follow that skill for shared utility reuse and maintenance.

### Compact forms
For every new or modified form, modal form, filter form, input, select, textarea, button, or form layout, follow `.agents/skills/crm-compact-form-ui/SKILL.md` and its relevant references. Apply compact form density by default, even when the request does not explicitly mention spacing or sizing. Also respect existing responsive and typography skills. Do not change unrelated screens, application behavior, or shared global CSS.

### Mobile button density
For any new or modified button, text action, tab, filter action or modal action on CRM frontend pages, follow `.agents/skills/crm-mobile-button-density/SKILL.md` alongside the existing UI skills. It defines a compact mobile-only baseline for button **height, font size, margin, and padding**; do not alter button width, colors, borders, icons, positioning, or behavior as part of density changes. The shared runtime CSS is `wwwroot/css/mobileButtonDensity.css`. Preserve icon-only, close, pagination, and shell/navigation controls and required touch ergonomics.

## ERP clean code and governance (all coding tasks)
- Load and follow [erp-clean-code](.agents/skills/erp-clean-code/SKILL.md) for every implementation, bug fix, refactoring, review, SQL or UI task; read the mandatory baseline and relevant numbered references. Use its before/after checklists for edits. Preserve all existing specialist skills and architecture rules, including mandatory CQRS for API/React migration as specified by crm-backend.
- Critical security, record Scope, data integrity and financial safeguards cannot be bypassed. Check direct and indirect access paths including reports, exports, search, files and real-time endpoints. Parameterize SQL; never trust client-side restrictions.
- Reuse existing helpers and components; verify callers before deleting or modifying shared contracts. Keep changes scoped, preserve valid behavior and do not introduce unnecessary recovery paths, packages or parallel architecture.
- Mandatory exceptions require documented authorization; recommended size/complexity metrics are guidance, not automatic blockers. Report actual build/test/lint results and unverified areas; never claim success without execution. No unauthorized migrations, live writes, commit, push or merge.
- Track governance rollout in [چک لیست قوانین](docs/coding-standards/RULES-CHECKLIST.md). Documented rules do not imply application-wide compliance or active CI enforcement.

### Onion Architecture quality gate
For every backend, cross-layer or project-reference change, load [rule 34](.agents/skills/erp-clean-code/references/34-onion-architecture.md) and [architecture review](.agents/skills/erp-clean-code/checklists/architecture-review.md). Run the read-only `scripts/check-onion-architecture.py` where possible; it checks direct layer project references only. Inspect source-level dependencies, DI, repository contracts, CQRS dispatcher, and vertical slices separately. API/React migration requires CQRS; existing legacy violations do not authorize new ones. Do not claim architecture compliance from the project-reference check alone. See [quality tooling setup](docs/coding-standards/QUALITY-TOOLS.md) for installation and staged enforcement.

## Access catalog entity acceptance and mandatory handoff

- For a catalog-enforced operation, authorize exclusively through effective permissions and record Scope (including individual grants, denies and exclusions), not a fixed role name. Roles group grants; they are not an additional authorization condition or bypass. Preserve authentication, verified account-type boundaries, ownership/participant relationships, business invariants and separation of duties. A legacy role condition may be used only while the corresponding replacement is disabled; for operations spanning multiple catalogs, preserve protection until every required replacement is enforced. Security administration without a catalog replacement must stay protected. Never substitute a broad View grant for an unmapped privileged action.
- For every entity, review role-dependent navigation (including nested settings and shared header controls), endpoints, CQRS, services, repositories, Razor/JS, SignalR, background jobs, reports/exports/files and test a granted user with a different role and an explicitly denied legacy-role user. Run `python scripts/audit-role-authority.py` to inventory remaining candidates. Its output is a review backlog, not proof of security or a completed migration.
Apply this ordered workflow to every access catalog entity. Work on the current entity until its handoff is accepted; do not start the next entity merely because a configuration flag is enabled.

1. Inventory the entity/model, related entities, existing roles and user access, all actual operations, direct and indirect entry points, and existing business constraints. Identify the relevant owner scopes and special actions.
2. Register the entity/model in the catalog so it appears in the entity administration tab. Define all applicable action keys and scopes, without inventing unused CRUD workflows.
3. Determine the exact legacy-equivalent grants for existing users through role membership and existing individual access rules. Preserve explicit user overrides and exclusions; never silently broaden grants to related entities.
4. Complete server enforcement for controllers, actions, APIs, CQRS handlers, services, repositories and database queries, including related records, shared operations, searches, reports, exports, attachments, bulk actions and background/real-time paths when applicable.
5. Complete frontend enforcement for menus, pages, buttons, forms, modals, shared selectors, dynamic controls and live revocation. Preserve authorized historical document snapshots and existing business rules; backend remains authoritative.
6. Review all direct and indirect paths against the inventory, check for privilege escalation and regressions, and run the full build, coding and Onion Architecture checks and relevant automated authorization tests. Run runtime SQL/browser tests on a test database when available. Record evidence and any genuine blockers; do not claim a passed test that was not run. Do not merge to main.
7. Only after the implementation, grant mapping and review are complete, prepare the final, scoped, idempotent, preview-first UserConnection registration/grant SQL script for the entity (or explicitly agreed group), and update the consolidated SQL consistently. Do not hand off a partial registration script while implementation work remains. Verify script coverage, compatibility with existing grants, and repeat execution safety. A private test fixture or test-only registration is not the final script handoff.
8. Automatically create, commit and deliver the final SQL script in the same entity workflow, without waiting for a separate user request to create it. If legacy grants are ambiguous, preserve existing grants and explicit overrides, choose the narrowest supported initial grants, and clearly disclose the assumption and any possible loss of legacy access; do not invent or silently widen access. Do not use an ambiguity alone to postpone generating the scoped script. Deliver the final script and exact execution instructions, commit reference, build/test status and any outstanding external validation in one handoff. Never execute the user's database writes without authorization. Wait for the user's confirmation that the final script was successfully applied; do not begin the next entity or ask the user to apply intermediate scripts for unfinished work.
9. After the user confirms successful SQL execution, automatically verify registration and effective permissions where access is available, perform the database-dependent acceptance checks, enable the reviewed Development enforcement flag, commit and push the activation, and run the applicable build/tests. Activation is a mandatory part of the same entity workflow and requires no separate user instruction or approval for Development. Never ask the user to request activation again. Do not enable Production flags or activate before SQL execution is confirmed. Confirm the entity is complete only to the extent supported by evidence. If a prerequisite or runtime security check is blocked, disclose it and resolve it or obtain the user's explicit acceptance of the limitation before progressing. Keep production configuration and main unchanged unless separately authorized.

Track catalog implementation, SQL application, effective grant verification, runtime tests and activation as distinct statuses. Never equate flag activation with security completion.
