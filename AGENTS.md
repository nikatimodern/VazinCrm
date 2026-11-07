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
When creating or editing UI (including design previews), load and follow [.agents/skills/project-ui-design-system/SKILL.md](.agents/skills/project-ui-design-system/SKILL.md) and its design-token reference for the official palette and component styling. Apply it alongside the frontend skills below; this does not authorize global restyling or changes outside the requested scope.

Use relevant skills in `.agents/skills/` when the task matches their descriptions. The repository's actual code and explicit user instructions take precedence over examples in skills.
For every frontend task in any module (including Razor, JavaScript, CSS, and frontend components), load `.agents/skills/crm-frontend-reusables/SKILL.md`. Follow that skill for shared utility reuse and maintenance.

### Compact forms
For every new or modified form, modal form, filter form, input, select, textarea, button, or form layout, follow `.agents/skills/crm-compact-form-ui/SKILL.md` and its relevant references. Apply compact form density by default, even when the request does not explicitly mention spacing or sizing. Also respect existing responsive and typography skills. Do not change unrelated screens, application behavior, or shared global CSS.
