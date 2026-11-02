---
name: crm-code-review
description: Review Vazin CRM changes for concrete bugs, security issues, regressions, EF Core query risks, frontend defects, and Sepidar/financial correctness without editing files.
---

# CRM Code Review

Use for reviews of diffs, commits, pull requests, or changed modules.

## Procedure
1. Determine the requested diff/base and inspect changed files plus directly related callers and contracts.
2. Check Onion Architecture boundaries: Domain must not depend on Infrastructure/EF Core/MVC, repository interfaces belong in Domain, SQL implementations and Fluent API in Infrastructure, and CQRS handlers in the existing `CRM.Core.ApplicationServices` layer. Prioritize reproducible defects: authorization bypass, data loss, duplicate transfers, broken EF translations, null errors, incorrect calculations, frontend lifecycle bugs, and API regressions.
3. Verify each finding against code. Avoid speculative style critiques and avoid repeating the same root cause.
4. Report findings in Persian, ordered by severity, with file path and line when available, triggering scenario, impact, and minimal fix suggestion.
5. Distinguish verified defects from assumptions and identify missing tests or unverified runtime behavior.
6. If no concrete findings exist, say so and note remaining test gaps.

For frontend changes, check compliance with [the vanilla JavaScript requirement](../crm-frontend/SKILL.md#vanilla-javascript-requirement): flag newly introduced jQuery application code or dependencies as an explicit project-rule violation. Distinguish these from unchanged legacy integrations; do not report existing jQuery usage outside the requested diff as a new defect.

## Guardrails
Review-only: do not modify files, commit, push, merge, or execute destructive actions unless explicitly asked in a separate instruction.

## Project-wide architecture and change policy
- Whenever CQRS is used, route Commands and Queries through the shared `ICqrsDispatcher`; controllers must not inject or invoke handlers directly. For fully converted controllers, every action must dispatch its operation and the Dispatcher must be the only injected dependency. Follow [the Dispatcher requirement](../crm-backend/SKILL.md#mandatory-dispatcher-for-cqrs).
- Follow the Feature-Based / Vertical Slice CQRS organization defined in `AGENTS.md` for all features: place each request, handler, and operation-specific DTO/result together under `<Feature>/Commands/<Operation>/` or `<Feature>/Queries/<Operation>/`. Do not introduce a `Features` wrapper or a feature-wide `Handlers` folder. Keep shared models in `<Feature>/Dtos` and generic CQRS contracts in `Abstractions/Cqrs`; match namespaces to paths.
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For new features, consider and suggest CQRS where beneficial, but use the existing `CRM.Core.ApplicationServices` layer; suggest CQRS where beneficial and obtain approval before introducing new CQRS patterns or dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
