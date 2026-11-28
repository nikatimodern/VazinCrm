# HRM — implementation checklist
Update only with evidence. Counts are task counts, not effort. Design is not implementation. Baseline: main at branch creation, 2026-11-28. See ARCHITECTURE.md and root AGENTS.md. AI Core checklist is on ai-core; HRM is first integration but is independent of AI availability.

## H01 — Preparation
- [x] Create hrm from main (GitHub branch creation confirmed).
- [x] Read root AGENTS and mandatory Clean Code baseline/Onion and backend skills.
- [x] Record initial architecture, logical entities and scoped documentation instructions.
- [x] Inspect AppUser, Identity/CRM contexts, admin employee/account queries, attachment entity and AccLevel catalog context difference (TECHNICAL-DESIGN.md).
- [ ] Trace approval workflows, protected attachment download and actual catalog enforcement; approved catalog integration required.
- [x] Inspect current employee-to-Sepidar mapping keys and AppUser position/person references; distinguish user and payroll mappings.
- [ ] Verify existing organization/personnel records, payroll schema and safe migration/reuse paths.
- [ ] Identify device brand/model, firmware, LAN access, API/SDK and anonymized sample log.
- [ ] Confirm business rules, calendar, time zone, payroll owner and current effective legal requirements.
- [x] Draft personnel, organization, device, raw attendance and work-rule relational schema and ingestion contract (TECHNICAL-DESIGN.md).
- [ ] Review design, relationships, retention, security, and obtain schema approval.
## H02 — Personnel and organization
- [x] Add five initial Domain entities with per-record constructor/date/reference validation: EmployeeProfile, EmploymentRecord, Department, JobPosition, EmployeeAssignment (uncompiled).
- [ ] Define repository contracts and SQL implementations, per-entity EF mappings and scoped application use cases; approved migration + SQL.
- [ ] Isolate HR configurations to CRMContextDb: SepidarContextDbBase currently scans the shared assembly without a predicate; verify both contexts before registration.
- [ ] Enforce employee profile/history uniqueness, optional Identity link existence/uniqueness and non-overlap transactionally; build workflow.
- [ ] Enforce department/manager ancestor cycle checks, position existence and primary assignment overlap transactionally; build workflow.
- [ ] Restricted personnel documents, audit and permission/scope registration.
- [ ] Compact RTL personnel/organization UI, search/report/export authorization and tests.
## H03 — Attendance devices
- [ ] Device catalog, credentials reference, health and sync status.
- [ ] Vendor-neutral adapter and first verified device implementation.
- [ ] Device-to-employee mapping and unmapped event queue.
- [ ] Append-only raw punches, deduplication, cursor transaction and retry/replay.
- [ ] Sync logs, timezone/clock reconciliation, offline recovery and multi-device tests.
- [ ] No biometric templates or destructive device operations by default.
## H04 — Work rules and attendance
- [ ] Work calendar, holidays and shift definition/assignment (fixed, rotating, flexible, overnight).
- [ ] Effective-dated rule sets, grace, breaks, daily hours, delay, early exit and absence.
- [ ] Daily attendance, incomplete punch detection, audited corrections and recalculation.
- [ ] Daily/monthly views, filters, reports and deterministic regression tests.
## H05 — Requests and self-service
- [ ] Leave types, entitlement ledger, hourly/daily and balances.
- [ ] Mission, overtime and attendance correction requests.
- [ ] Manager/HR approvals, rejections, separation of duties and notifications.
- [ ] Personal portal, own documents/attendance/requests, scoped access and mobile UI.
- [ ] Monthly review, approved snapshot, close/lock, authorized reopen and audit.
## H06 — Contracts and payroll
- [ ] Contracts, amendments, expiry alerts and compensation components.
- [ ] Confirm payroll system of record (ERP or Sepidar), calculation/export ownership.
- [ ] Effective-dated legal rules, verified payroll, insurance, tax, benefits and deductions if ERP owns calculation.
- [ ] Loans/advances, installments, payslips and controlled access.
- [ ] Sepidar mapping/export, reconciliation, idempotency and approved human execution.
- [ ] Offboarding and settlement with financial review.
## H07 — Extended HR
- [ ] Recruitment and onboarding with human decisions.
- [ ] Training, certificates and development plans.
- [ ] Human-led performance/KPI, job-context review and fair access.
- [ ] Equipment/assets assigned to employees and handover/return.
- [ ] HR dashboard, department/period reports, contract expiry and work capacity.
## H08 — AI integration (first module)
- [ ] Agree normalized contract with ai-core; HRM works offline/without AI.
- [ ] Read-only attendance summaries, anomaly explanations, policy Q&A, source links.
- [ ] Overtime trends, aggregate shift capacity and cost insights with human review.
- [ ] HR documents and employee assistant under data policy; sensitive data LocalOnly after VM readiness.
- [ ] HR agents, scheduled findings, notifications and permission checks.
- [ ] No AI calculation of pay/leave, automated hiring, firing, discipline or pay decisions.
## H09 — Verification and release
- [ ] Unit/integration tests, shift/holiday/overnight, missing/duplicate punch, concurrency, month lock.
- [ ] Scope/permission, export/file/report/notification isolation, privacy and audit tests.
- [ ] Device outage/replay, payroll/Sepidar reconciliation and AI unavailable tests.
- [ ] Architecture checker, actual build, UI/RTL/mobile review, documentation and pilot.
- [ ] Approved cross-branch integration and release; no main merge without approval.

Evidence: v0.3 five HumanResources Domain entities added and read back from hrm. Per-record guards only; uniqueness, cross-record dates, cycles and permission require persistence/application checks. EmploymentRecord is the source of employment history; no duplicate employment state on EmployeeProfile yet. No EF mapping registration, migration, device connection, UI, endpoint, or main changes. Shared assembly scanning in Sepidar requires context isolation before HR configurations. No checked-out repository or dotnet SDK; build and tests not run.
