---
name: crm-frontend
description: Build and improve Vazin CRM Razor and RTL interfaces, JavaScript components, and React interfaces for the gradual API migration.
---

# CRM Frontend

Use for Razor, JavaScript, CSS, modal, select, table, form, and frontend refactoring work.

## API and React migration
- Every migrated feature must have CQRS-backed API operations before the migration is considered complete. Follow the mandatory migration requirements in [CRM Backend](../crm-backend/SKILL.md#mandatory-cqrs-for-api-and-react-migration), including login and user-session operations.
- React consumes API contracts; keep business rules, authorization enforcement, and persistence orchestration in the appropriate backend layers. Reuse the existing CQRS pattern without asking for approval per migrated feature; new dependencies still require approval.

## Workflow
1. Inspect the affected view/partial, JS, CSS, and page layout. Follow [CRM Frontend Reusables](../crm-frontend-reusables/SKILL.md) for shared utilities and HTTP requests.
2. Prefer native JavaScript and DOM APIs. Use jQuery for genuine plugin interoperability, especially DataTables, not ordinary event handling or selectors.
3. Use TomSelect for enhanced selects. Never add new Select2 integrations; consider migrating existing Select2 controls when touching them if practical and scoped.
4. Write scoped, component-specific CSS using existing design tokens and RTL conventions. Use Bootstrap's interactive JavaScript components, such as modals, and suitable lightweight utility classes; prefer custom visual styling over default Bootstrap component appearance.
5. Check TomSelect lifecycle, dropdown positioning, modal overflow/scroll, focus, cleanup, and repeated modal opening.
6. Prevent duplicate listeners, repeated submissions, stale state, excessive DOM updates, and unnecessary allocations.
7. Favor concise, readable, performant implementations. Refactor related touched code when it improves clarity, reuse, or efficiency without broad unrelated changes.

## Verification
- Check Persian RTL, mobile layout, keyboard interaction, loading/errors, empty states, and repeated use when practical.
- Keep existing API contracts and server validation intact.
- Use short English JSDoc when useful; report changes and actual verification in Persian.

## Guardrails
Do not introduce new frontend dependencies or rewrite global styles without a concrete need and approval.

## Project-wide architecture and change policy
- Whenever CQRS is used, route Commands and Queries through the shared `ICqrsDispatcher`; controllers must not inject or invoke handlers directly. For fully converted controllers, every action must dispatch its operation and the Dispatcher must be the only injected dependency. Follow [the Dispatcher requirement](../crm-backend/SKILL.md#mandatory-dispatcher-for-cqrs).
- Follow the Feature-Based / Vertical Slice CQRS organization defined in `AGENTS.md` for all features: place each request, handler, and operation-specific DTO/result together under `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/`. Do not introduce a `Features` wrapper or a feature-wide `Handlers` folder. Keep shared models in `<Feature>/Dtos` and generic CQRS contracts in `Abstractions/Cqrs`; match namespaces to paths.
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For migrated API and React features, CQRS is mandatory as described above. Outside that migration, consider and suggest CQRS where beneficial using the existing `CRM.Core.ApplicationServices` layer. Obtain approval before introducing a different CQRS pattern or new dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
