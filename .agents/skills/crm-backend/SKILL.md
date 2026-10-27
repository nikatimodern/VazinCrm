---
name: crm-backend
description: Implement ASP.NET Core MVC endpoints and layered Onion Architecture backend features for Vazin CRM, including future CQRS handlers and domain repository contracts.
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

## Workflow
- Use Feature-Based / Vertical Slice organization by default for every new Command and Query across all features, not only Reports. Place features directly under `CRM.Core.ApplicationServices`, without a `Features` wrapper folder.
- Give each operation a separate `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/` folder. Keep its request, handler, and operation-specific DTO/result together; never use a feature-wide `Handlers` folder. Put only models shared by multiple operations in `<Feature>/Dtos/`.
- Match namespaces to these folders (for example, `CRM.Core.ApplicationServices.Reports.Commands.AddReport`). Name requests, handlers, and results after actual behavior: `GetAllReportsQuery` retrieves all reports, while `GetReportQuery` retrieves one. Keep generic CQRS contracts in `Abstractions/Cqrs`.
- Preserve existing CQRS contracts, DI lifetimes, HTTP responses, authorization, and business logic. Update namespaces, usings, registrations, and callers when moving files. Do not add unnecessary files, interfaces, abstractions, or packages; new packages require approval.
1. Trace the existing request path, DI registrations, and domain behavior.
2. Choose the correct layer for each responsibility before editing.
3. For every new feature, evaluate CQRS suitability and propose it briefly before implementation where useful. The `CRM.Core.ApplicationServices` project already exists. Inspect its actual structure and dependencies, and obtain approval before introducing a new CQRS pattern or package.
4. For every new feature with persistence, define a repository interface in Domain and implement it in SQL Infrastructure; this boundary is mandatory even for simple queries. For approved CQRS work, put Command/Query handlers and orchestration in ApplicationService.
5. Preserve existing HTTP contracts, authorization, validation, cancellation, and async behavior.
6. Refactor touched code for clarity, performance, and reuse. For broader refactoring, first propose the affected files, rationale, risks, and verification steps, and wait for approval.
7. Run focused checks and summarize changes in Persian.

## Guardrails
Do not recreate the existing ApplicationServices layer or add a new layer, change dependency direction, weaken authorization, or migrate unrelated legacy services without approval. Avoid unnecessary abstractions unrelated to a real boundary.
