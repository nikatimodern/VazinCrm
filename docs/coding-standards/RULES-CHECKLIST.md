# چک لیست قوانین

Status tracks written governance, not application-wide compliance. A checked section means its reference is documented. Operational enforcement, build and tooling activation are tracked separately.

- [x] 01 [Core principles](../../.agents/skills/erp-clean-code/references/01-core-principles.md) — Mandatory
- [x] 02 [SOLID](../../.agents/skills/erp-clean-code/references/02-solid.md) — Mandatory with justified exceptions
- [x] 03 [Naming](../../.agents/skills/erp-clean-code/references/03-naming.md) — Mandatory
- [x] 04 [Methods](../../.agents/skills/erp-clean-code/references/04-methods.md) — Mandatory; size targets recommended
- [x] 05 [Architecture](../../.agents/skills/erp-clean-code/references/05-architecture.md) — Mandatory
- [x] 06 [Code reuse](../../.agents/skills/erp-clean-code/references/06-code-reusability.md) — Mandatory
- [x] 07 [Exception handling](../../.agents/skills/erp-clean-code/references/07-exception-handling.md) — Mandatory
- [x] 08 [Database and EF Core](../../.agents/skills/erp-clean-code/references/08-database-ef-core.md) — Mandatory
- [x] 09 [Async and performance](../../.agents/skills/erp-clean-code/references/09-async-performance.md) — Mandatory; optimizations evidence-based
- [x] 10 [Frontend](../../.agents/skills/erp-clean-code/references/10-frontend.md) — Mandatory
- [x] 11 [Security and authorization](../../.agents/skills/erp-clean-code/references/11-security.md) — Critical
- [x] 12 [Comments and documentation](../../.agents/skills/erp-clean-code/references/12-comments.md) — Mandatory
- [x] 13 [Code cleanup](../../.agents/skills/erp-clean-code/references/13-code-cleanup.md) — Mandatory
- [x] 14 [Testing](../../.agents/skills/erp-clean-code/references/14-testing.md) — Mandatory
- [x] 15 [Git and change management](../../.agents/skills/erp-clean-code/references/15-git.md) — Mandatory
- [x] 16 [Quality metrics](../../.agents/skills/erp-clean-code/references/16-quality-metrics.md) — Recommended except zero new build errors and critical defects
- [x] 17 [Project instructions](../../.agents/skills/erp-clean-code/references/17-project-instructions.md) — Mandatory
- [x] 18 [Skill structure](../../.agents/skills/erp-clean-code/references/18-skill-structure.md) — Mandatory
- [x] 19 [Rule classification](../../.agents/skills/erp-clean-code/references/19-rule-classification.md) — Mandatory
- [x] 20 [Development workflow](../../.agents/skills/erp-clean-code/references/20-development-workflow.md) — Mandatory
- [x] 21 [ERP-specific standards](../../.agents/skills/erp-clean-code/references/21-erp-standards.md) — Mandatory
- [x] 22 [Automated quality control](../../.agents/skills/erp-clean-code/references/22-automated-quality-control.md) — Mandatory for applicable checks; rollout staged
- [x] 23 [Review checklist](../../.agents/skills/erp-clean-code/references/23-review-checklist.md) — Mandatory
- [x] 24 [Data integrity](../../.agents/skills/erp-clean-code/references/24-data-integrity.md) — Critical
- [x] 25 [Financial standards](../../.agents/skills/erp-clean-code/references/25-financial-standards.md) — Critical
- [x] 26 [API standards](../../.agents/skills/erp-clean-code/references/26-api-standards.md) — Mandatory
- [x] 27 [External integrations](../../.agents/skills/erp-clean-code/references/27-external-integrations.md) — Critical
- [x] 28 [Logging and audit](../../.agents/skills/erp-clean-code/references/28-logging-audit.md) — Mandatory; confidentiality Critical
- [x] 29 [Configuration and secrets](../../.agents/skills/erp-clean-code/references/29-configuration.md) — Critical
- [x] 30 [Concurrency and idempotency](../../.agents/skills/erp-clean-code/references/30-concurrency.md) — Critical for sensitive writes
- [x] 31 [Backward compatibility](../../.agents/skills/erp-clean-code/references/31-backward-compatibility.md) — Mandatory
- [x] 32 [UI/UX standards](../../.agents/skills/erp-clean-code/references/32-ui-ux-standards.md) — Mandatory
- [x] 33 [Dependency management](../../.agents/skills/erp-clean-code/references/33-dependency-management.md) — Mandatory

- [x] 34 [Onion Architecture](../../.agents/skills/erp-clean-code/references/34-onion-architecture.md) — Mandatory

## Rollout and verification
- [x] Existing root instructions and specialist skills inspected; preserve them.
- [x] Dedicated skill, 34 references and seven review checklists authored.
- [x] Root instruction integration, EditorConfig and structural validator authored.
- [x] Onion Architecture reference, checklist and project-reference checker authored.
- [ ] Execute project-reference checker in a checked-out repository and review source-level boundaries.
- [ ] Execute structural validator in a checked-out repository.
- [ ] Verify skill discovery with a real Codex session.
- [ ] Run solution build and applicable existing tests in configured SDK environment.
- [ ] Establish analyzer/format baseline and review new diagnostics.
- [ ] Configure and verify JS lint/format, dependency scanning and SonarQube as applicable.
- [ ] Activate and verify CI quality gate and branch protection with repository owner.
- [ ] Review branch and merge after explicit authorization.

No production code or database changes are authorized by this checklist.
