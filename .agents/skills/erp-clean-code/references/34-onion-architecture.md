# 34. Onion Architecture — project-specific mandatory standard

**Classification:** Mandatory for new features and changed boundaries; critical security/data invariants are never waived. Existing legacy deviations must be identified, not spread or rewritten outside scope.

## Required behavior
### Dependency direction and ownership
Dependencies point toward the core. Use the existing project references and maintain this permitted direct project-reference matrix:

| Source project | Permitted project references |
| --- | --- |
| CRM.Framework | None of the business/application/infrastructure/endpoint projects |
| CRM.Core.Domain | CRM.Framework |
| CRM.Core.ApplicationServices | CRM.Core.Domain (Framework only if explicitly needed and consistent with architecture) |
| CRM.Infrastructures.Sql | CRM.Core.Domain, CRM.Framework |
| CRM.EndPoint.FrontEnd | CRM.Core.ApplicationServices, CRM.Core.Domain, CRM.Infrastructures.Sql, CRM.Framework (infrastructure for composition and existing scoped legacy use) |

- Framework is domain-independent; do not move business-specific services into shared framework.
- Domain owns entities, value objects, enums, invariants, business rules and repository interfaces. No DbContext, EF Core, SQL, HTTP/MVC, Razor, concrete external clients or application/outer references. Existing Identity contracts are a legacy dependency; do not misrepresent or expand them without review.
- ApplicationServices owns use cases, orchestration, commands/queries/handlers and operation DTOs. Depend on Domain contracts, not SQL implementations, EF queries, MVC types or HTTP responses. Keep business permission and scope checks at the appropriate application boundary; never rely on endpoint/UI alone.
- SQL Infrastructure owns DbContext, entity mappings, migrations, LINQ provider operations, SQL and repository implementations. It implements inward contracts. Keep provider-specific types and IQueryable that leaks persistence concerns out of new repository contracts.
- FrontEnd owns HTTP binding, endpoint authorization, HTTP response mapping, Razor/JS/UI and composition root. Register interfaces and implementations with compatible lifetimes at composition; never resolve concrete infrastructure from new controllers or inject DbContext in new controllers/handlers.
- For external systems define needed inward-facing contracts at the appropriate core boundary, keep concrete SDK/network/persistence adapters in appropriate outer infrastructure according to current repository organization; do not invent new projects or frameworks without approval.

### CQRS and feature organization
- Use existing ICqrsDispatcher for all CQRS entry points; never inject or invoke concrete handlers directly in controllers. Fully converted controllers have only dispatcher dependency and dispatch every action, including meaningful page data queries.
- All API/React migration features must use CQRS, including authentication, reports and financial operations, as already authorized. Outside migration assess CQRS suitability and follow existing approval policy.
- Place each operation in CRM.Core.ApplicationServices/<Feature>/Commands/<Operation>/ or Queries/<Operation>/; colocate request, handler and operation result. Shared DTOs only when genuinely reused. Keep generic contracts in Abstractions/Cqrs; no Features wrapper or feature-wide Handlers folder.
- Commands represent state changes; queries represent reads and must not cause hidden business mutations. Use explicit request models, validate inputs, pass CancellationToken and return application-level results.
- For every new persistence feature define repository interface in Domain and implement it in SQL Infrastructure; use focused business-oriented contracts rather than generic wrappers without need. Keep transaction boundaries explicit; avoid independent SaveChanges that can leave partial writes. Do not assume cross-database atomicity.
- Enforce business invariants and authorized scope across entry points, use consistent validation, ensure external writes are idempotent and auditable. Keep mapping at the boundary and avoid returning tracked entities to HTTP clients where DTOs are appropriate.

## Prohibited behavior and exceptions
No outward or cyclic project references, direct SQL/EF from new application or Domain code, business logic in new controllers/views, direct handler calls from controllers, service locator in feature code, or new generic abstractions merely for metrics. Composition root may reference concrete outer implementations for DI. Existing legacy controller/infrastructure dependencies are scoped exceptions, not permission to copy them. Preserve external contracts and request approval for broad migration or new packages. Do not change project references or runtime behavior just to make a governance test pass.

## Examples
Good: HTTP action -> ICqrsDispatcher -> ApproveInvoiceCommand handler -> Domain invoice/repository contract -> SQL repository -> DbContext (registered by composition root). Domain invariant guards approval; handler coordinates access and transaction; controller maps result to HTTP.

Bad: Domain references CRM.Infrastructures.Sql; application handler uses CRMContextDb directly; controller calculates tax, calls handler directly and writes invoice; Razor decides actual permissions.

Good repository contract: Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken); SQL implementation decides tracking/projection appropriate to use case. For list reads prefer focused application/domain-compatible result contracts; never return provider-specific IQueryable as a new boundary.

## Review checklist
- [ ] Inspect actual csproj references, source dependencies and namespaces; confirm inward direction and absence of cycles.
- [ ] Verify Domain contracts/invariants and Infrastructure implementations, EF mappings, correct context and transaction ownership.
- [ ] Verify application handlers, command/query split, DI lifetimes, cancellation and absence of direct EF/HTTP dependencies.
- [ ] Verify every converted endpoint dispatches its application operation, retains authorization/Scope and maps results without business logic.
- [ ] Verify vertical slice organization, DTO boundaries, compatibility, integration/financial safeguards and test coverage.
- [ ] Run read-only project reference checker and applicable build/test; report exact outcomes and remaining legacy source-level violations. A project-reference check alone does not prove architectural compliance.
