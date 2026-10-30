---
name: crm-backend
description: Implement ASP.NET Core MVC and API endpoints, Onion Architecture backend features, and mandatory CQRS operations for Vazin CRM features migrated to API and React.
---

# CRM Backend and Onion Architecture

Use for controllers, endpoints, business logic, repository contracts, and CQRS-related work.

## Layer placement
- `CRM.Framework`: domain-independent shared utilities.
- `CRM.Core.Domain`: entities, invariants, domain behavior, repository **interfaces**.
- `CRM.Core.ApplicationServices`: existing application layer for CQRS commands, queries, handlers, and application orchestration; inspect actual dependencies and DI registrations before editing.
- `CRM.Infrastructures.Sql`: EF Core, Fluent API, database access, repository **implementations**.
- `CRM.EndPoint.FrontEnd`: controllers, Razor, HTTP mapping, dependency injection and UI.

Dependencies point inward. Do not reference SQL, EF Core or MVC from Domain. Keep controllers thin. Use Domain repository interfaces in application code rather than concrete SQL implementations.

## Mandatory Dispatcher for CQRS
- Whenever an operation uses CQRS, controllers and other entry points must send its Command or Query through the existing `ICqrsDispatcher`. Do not inject `ICommandHandler<,>` or `IQueryHandler<,>` into controllers or call their `HandleAsync` methods directly.
- For a controller requested to be fully converted to CQRS, `ICqrsDispatcher` must be its only injected dependency and every action must dispatch its application operation. Page initialization such as `Index` should dispatch a meaningful page-data Query and pass the result to the view; do not add empty handlers just to satisfy this rule.
- Keep only HTTP binding, request/result mapping, MVC rendering or file responses, and HTTP error mapping in controllers. Move user lookup, role-dependent business decisions, persistence, auditing, integration calls, and notifications into handlers and the appropriate application contracts. Preserve endpoint authorization attributes and enforce business permissions server-side.
- Place the Dispatcher contract in `ApplicationServices/Abstractions/Cqrs` and keep DI-based handler resolution in the composition layer. Reuse the shared Dispatcher rather than introducing a feature-specific one. Forward `CancellationToken` and preserve scoped handler lifetimes.
- Verify that each converted action uses the Dispatcher and that no business helper methods or direct DbContext/service dependencies remain in a fully converted controller.

## Mandatory CQRS for API and React migration
- Every feature migrated from the existing MVC/Razor application to API and React must use CQRS for its backend application operations. This is a migration requirement, not an optional suitability assessment, including authentication, dashboard, reports, and financial features.
- Model reads as Queries and state-changing operations as Commands, each with its handler in the existing `CRM.Core.ApplicationServices` project. Reuse existing handlers when their behavior matches; do not create duplicate handlers or empty pass-through operations for presentation-only changes.
- Keep API endpoints thin: map HTTP requests and results, enforce authorization and validation, and delegate application work to handlers. Do not move legacy controller business logic or direct DbContext access into new API endpoints.
- Use the Feature-Based / Vertical Slice folders and `Abstractions/Cqrs` contracts below. Handlers use Domain repository contracts for persistence; their implementations belong in SQL Infrastructure. Preserve Identity security behavior through appropriate application contracts without introducing EF Core or MVC dependencies into handlers.
- The user has authorized adopting the existing CQRS pattern for this migration; do not request approval again per feature. New packages, a different CQRS framework, or unrelated broad refactoring still require approval.
- A migrated feature is not complete until its backend operations use CQRS and its React callers use the corresponding API contracts. Verify authorization, business behavior, and HTTP results against the existing feature.

## Workflow
- Use Feature-Based / Vertical Slice organization by default for every new Command and Query across all features, not only Reports. Place features directly under `CRM.Core.ApplicationServices`, without a `Features` wrapper folder.
- Give each operation a separate `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/` folder. Keep its request, handler, and operation-specific DTO/result together; never use a feature-wide `Handlers` folder. Put only models shared by multiple operations in `<Feature>/Dtos/`.
- Match namespaces to these folders (for example, `CRM.Core.ApplicationServices.Reports.Commands.AddReport`). Name requests, handlers, and results after actual behavior: `GetAllReportsQuery` retrieves all reports, while `GetReportQuery` retrieves one. Keep generic CQRS contracts in `Abstractions/Cqrs`.
- Preserve existing CQRS contracts, DI lifetimes, HTTP responses, authorization, and business logic. Update namespaces, usings, registrations, and callers when moving files. Do not add unnecessary files, interfaces, abstractions, or packages; new packages require approval.
1. Trace the existing request path, DI registrations, and domain behavior.
2. Choose the correct layer for each responsibility before editing.
3. For API and React migration, apply the mandatory CQRS requirement above. Outside that migration, evaluate CQRS suitability for new features and propose it where useful. The `CRM.Core.ApplicationServices` project already exists; inspect its actual structure and dependencies before implementation. Obtain approval before introducing a different CQRS pattern or a new package.
4. For every new feature with persistence, define a repository interface in Domain and implement it in SQL Infrastructure; this boundary is mandatory even for simple queries. For approved CQRS work, put Command/Query handlers and orchestration in ApplicationService.
5. Preserve existing HTTP contracts, authorization, validation, cancellation, and async behavior.
6. Refactor touched code for clarity, performance, and reuse. For broader refactoring, first propose the affected files, rationale, risks, and verification steps, and wait for approval.
7. Run focused checks and summarize changes in Persian.

## Guardrails
Do not recreate the existing ApplicationServices layer or add a new layer, change dependency direction, weaken authorization, or migrate unrelated legacy services without approval. Avoid unnecessary abstractions unrelated to a real boundary.
