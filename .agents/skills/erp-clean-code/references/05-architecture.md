# 05. Architecture

**Classification:** Mandatory

## Required behavior
Follow AGENTS.md and crm-backend: Onion dependencies point inward. Domain owns invariants and repository contracts; SQL implements persistence; ApplicationServices orchestrates; FrontEnd maps HTTP and composes DI. New persistence features require Domain repository contracts; CQRS uses shared ICqrsDispatcher and vertical slices. API/React migration requires CQRS.

## Prohibited behavior and exceptions
No new direct DbContext access from controllers/handlers, no EF/MVC in Domain, no direct handler invocation from controllers. Legacy scoped fixes need not trigger full migration; broader rewrites and new frameworks require approval.

## Examples
Good: controller dispatches a command whose handler calls a Domain contract; bad: controller embeds tax rules and SQL writes.

## Review checklist
- [ ] Review project references, DI lifetimes, feature folders and every converted action.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
