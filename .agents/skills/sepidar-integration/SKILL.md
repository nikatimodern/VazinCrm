---
name: sepidar-integration
description: Implement and audit CRM-to-Sepidar transfers, Sepidar1/Sepidar2 service selection, external mappings, quotations, invoices, stock, customers, and accounting exports safely.
---

# Sepidar Integration

Use for any transfer, mapping, or lookup involving Sepidar or ExternalSoftware.

## Procedure
1. Inspect `Services/SepidarServices/SepidarService.cs`, `Sepidar1Service.cs`, `Sepidar2Service.cs`, and current DI registrations. The shared service currently uses `SepidarContextDbBase`; verify actual code before assuming its shape.
2. Trace external software selection and the exact target context before querying or writing.
3. Inspect `ExternalMapping`, entity mapping type, external ID type, and existing duplicate-transfer checks.
4. Validate required customer, item, sale type, stock, cost center, DL, and fiscal-year references as applicable to the operation; do not assume every operation needs all fields.
5. Keep source and target IDs distinct. Parse string external IDs safely and fail clearly when invalid.
6. Plan idempotency, retry behavior, partial failures, and reconciliation when CRM and Sepidar writes cannot be atomic.
7. Preserve Sepidar schema and verify actual EF mappings before using any column or relation.
8. Report transfer prerequisites, changed mapping behavior, and unverified external assumptions.

## Guardrails
Never run destructive or production writes without approval. Do not invent accounting codes, stock references, or SQL columns. Never bypass duplicate checks to make a transfer succeed.

## Project-wide architecture and change policy
- Whenever CQRS is used, route Commands and Queries through the shared `ICqrsDispatcher`; controllers must not inject or invoke handlers directly. For fully converted controllers, every action must dispatch its operation and the Dispatcher must be the only injected dependency. Follow [the Dispatcher requirement](../crm-backend/SKILL.md#mandatory-dispatcher-for-cqrs).
- Follow the Feature-Based / Vertical Slice CQRS organization defined in `AGENTS.md` for all features: place each request, handler, and operation-specific DTO/result together under `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/`. Do not introduce a `Features` wrapper or a feature-wide `Handlers` folder. Keep shared models in `<Feature>/Dtos` and generic CQRS contracts in `Abstractions/Cqrs`; match namespaces to paths.
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For new features, consider and suggest CQRS where beneficial, but use the existing `CRM.Core.ApplicationServices` layer; suggest CQRS where beneficial and obtain approval before introducing new CQRS patterns or dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
