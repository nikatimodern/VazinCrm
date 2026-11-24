---
name: erp-clean-code
description: Apply Vazin ERP coding standards to all implementation, bug fixes, refactoring, reviews, database, security, financial, frontend and integration changes; use alongside existing specialist skills.
---

# ERP Clean Code

Read repository AGENTS.md first. This skill supplements, not replaces, existing specialist skills and local instructions. Always read rules 01, 11, 19, 20 and 23; load other references based on affected code. For comprehensive audits read all 33. Critical rules cannot be waived; mandatory exceptions require documented authorization; recommended metrics are targets, not arbitrary blockers. Do not modify unrelated code, run live writes, or report checks not performed.

## Reference index
- [01 Core principles](references/01-core-principles.md)
- [02 SOLID](references/02-solid.md)
- [03 Naming](references/03-naming.md)
- [04 Methods](references/04-methods.md)
- [05 Architecture](references/05-architecture.md)
- [06 Code reuse](references/06-code-reusability.md)
- [07 Exception handling](references/07-exception-handling.md)
- [08 Database and EF Core](references/08-database-ef-core.md)
- [09 Async and performance](references/09-async-performance.md)
- [10 Frontend](references/10-frontend.md)
- [11 Security and authorization](references/11-security.md)
- [12 Comments and documentation](references/12-comments.md)
- [13 Code cleanup](references/13-code-cleanup.md)
- [14 Testing](references/14-testing.md)
- [15 Git and change management](references/15-git.md)
- [16 Quality metrics](references/16-quality-metrics.md)
- [17 Project instructions](references/17-project-instructions.md)
- [18 Skill structure](references/18-skill-structure.md)
- [19 Rule classification](references/19-rule-classification.md)
- [20 Development workflow](references/20-development-workflow.md)
- [21 ERP-specific standards](references/21-erp-standards.md)
- [22 Automated quality control](references/22-automated-quality-control.md)
- [23 Review checklist](references/23-review-checklist.md)
- [24 Data integrity](references/24-data-integrity.md)
- [25 Financial standards](references/25-financial-standards.md)
- [26 API standards](references/26-api-standards.md)
- [27 External integrations](references/27-external-integrations.md)
- [28 Logging and audit](references/28-logging-audit.md)
- [29 Configuration and secrets](references/29-configuration.md)
- [30 Concurrency and idempotency](references/30-concurrency.md)
- [31 Backward compatibility](references/31-backward-compatibility.md)
- [32 UI/UX standards](references/32-ui-ux-standards.md)
- [33 Dependency management](references/33-dependency-management.md)

## Workflow
Use [before development](checklists/before-development.md) and [after development](checklists/after-development.md) for all changes. Load [code review](checklists/code-review.md), [security](checklists/security-review.md), [database](checklists/database-review.md) and [refactoring](checklists/refactoring-review.md) when applicable. For reviews, follow existing [crm-code-review](../crm-code-review/SKILL.md). For backend/CQRS use [crm-backend](../crm-backend/SKILL.md); for frontend use [crm-frontend](../crm-frontend/SKILL.md) and AGENTS.md UI skill list; for persistence use [efcore-sqlserver](../efcore-sqlserver/SKILL.md); for finance/integrations use [crm-financial](../crm-financial/SKILL.md) and [sepidar-integration](../sepidar-integration/SKILL.md). Specialist rules govern their exact implementation; security/data safeguards remain mandatory. Report unresolved conflicts instead of guessing.

Governance status: [Rules checklist](../../../docs/coding-standards/RULES-CHECKLIST.md). Report changed files, relevant checks and actual outcomes in Persian.
