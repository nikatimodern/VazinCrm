---
name: crm-frontend
description: Build, improve, and refactor Vazin CRM Razor views, RTL UI, vanilla JavaScript, TomSelect, DataTables, scoped CSS, and reusable frontend components.
---

# CRM Frontend

Use for Razor, JavaScript, CSS, modal, select, table, form, and frontend refactoring work.

## Workflow
1. Inspect the affected view/partial, JS, CSS, and relevant public helpers in `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/site.js`.
2. Search for reusable JavaScript classes and utilities already implemented. Reuse them only when they simplify the task; avoid unnecessary abstraction.
3. Prefer native JavaScript and DOM APIs. Use jQuery for genuine plugin interoperability, especially DataTables, not ordinary event handling or selectors.
4. Use TomSelect for enhanced selects. Never add new Select2 integrations; consider migrating existing Select2 controls when touching them if practical and scoped.
5. Prefer `sendApiRequest` for application HTTP calls, and reuse actual existing Toastify notification, loading, validation, and formatting helpers where appropriate. Confirm exact names and signatures instead of guessing.
6. Write scoped, component-specific CSS using existing design tokens and RTL conventions. Use Bootstrap's interactive JavaScript components, such as modals, and suitable lightweight utility classes; prefer custom visual styling over default Bootstrap component appearance.
7. Check TomSelect lifecycle, dropdown positioning, modal overflow/scroll, focus, cleanup, and repeated modal opening.
8. Prevent duplicate listeners, repeated submissions, stale state, excessive DOM updates, and unnecessary allocations.
9. Favor concise, readable, performant implementations. Refactor related touched code when it improves clarity, reuse, or efficiency without broad unrelated changes.

## Verification
- Check Persian RTL, mobile layout, keyboard interaction, loading/errors, empty states, and repeated use when practical.
- Keep existing API contracts and server validation intact.
- Use short English JSDoc when useful; report changes and actual verification in Persian.

## Guardrails
Do not introduce new frontend dependencies or rewrite global styles without a concrete need and approval.

## Project-wide architecture and change policy
- Place application modules directly under the `CRM.Core.ApplicationServices` project root (for example, `Reports/Queries`, `Reports/Commands`, `Reports/Handlers`, and `Reports/Dtos` as needed). Do not introduce a `Features` wrapper folder. Match namespaces to these paths (for example, `CRM.Core.ApplicationServices.Reports.Queries`); keep shared CQRS contracts in `Abstractions/Cqrs`.
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For new features, consider and suggest CQRS where beneficial, but use the existing `CRM.Core.ApplicationServices` layer; suggest CQRS where beneficial and obtain approval before introducing new CQRS patterns or dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
