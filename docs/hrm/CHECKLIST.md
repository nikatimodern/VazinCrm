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
- [x] Add five per-entity EF configurations, named CRMContextDb sets, and bounded employee directory/history/assignment read repository contract and SQL implementation; source only.
- [ ] Complete scoped application use cases and transactional write repository; apply only an approved and verified migration.
- [x] Author CRMContextDb migration 20261128090000_AddHumanResources, Designer, additive snapshot update, and review SQL after explicit user request. Read-back structural checks: six new HR tables, no existing-table mutation in Up, Designer matches Snapshot; SDK/runtime validation pending.
- [ ] Run build, EF pending-model-changes check and generate/compare official EF SQL against actual baseline; test on a staging copy before production application.
- [x] Exclude the new AI/HR CRM-only configuration namespaces from Sepidar assembly scanning; verify matching source on both branches.
- [ ] Verify actual CRM and Sepidar EF models and repository SQL translation with build/tests.
- [ ] Enforce employee profile/history uniqueness, optional Identity link existence/uniqueness and non-overlap transactionally; build workflow.
- [ ] Enforce department/manager ancestor cycle checks, position existence and primary assignment overlap transactionally; build workflow.
- [ ] Restricted personnel documents, audit and permission/scope registration.
- [ ] Compact RTL personnel/organization UI, search/report/export authorization and tests.
## H02-C — Priority release: employee list, contracts and expiry reminders
**User-approved priority:** complete H02 and H02-C before H03/H04 attendance work. Keep all later modules in the roadmap. No dependence on AI or attendance devices.
- [x] Add distinct EmploymentContract entity, draft/approval/cancellation/termination transitions and linked renewal draft that preserves the predecessor (source only).
- [x] Add deterministic date-state and reminder-threshold policy (company-local date supplied by caller; inclusive end date; untested).
- [x] Add CRM-only EF mapping, unique contract number, employee/predecessor relations, checks and named DbSet (source only).
- [x] Add scoped, paged contract history and date-range read repository (source only).
- [x] Add HR menu, administrator-only preview workspace, paged employee list and create/edit modal source with top-of-list add button and existing-account selection; no production data enabled.
- [x] Extend employee profile with optional identity/contact/education/emergency contact fields, Persian birth-date parsing, national-code validation and uniqueness, and an additive CRM-only migration (20261128113200_ExpandEmployeeProfile); source and migration artifacts only.
- [x] Add employee profile detail, bounded account candidate lookup (IsEmployee, explicit authorized account scope, excludes linked accounts), create/edit repository and CQRS handler source; rowversion and unique constraint conflict handling; no Identity writes.
- [ ] Complete Access Catalog-backed scope and import authorization, scalable account search, actual DB migration, build and employee workflow tests.
- [x] Implement contract-specific command and serializable SQL write repository for draft create/edit, approval, linked renewal, cancellation and termination, overlap/predecessor checks, audit and rowversion conflict handling (source only).
- [ ] Verify actual transactional behavior, concurrent approvals and renewals, permission enforcement with the real Access Catalog, and employee employment/organization write rules.
- [x] Add read-only employee contract history section with independent contract-view scope, Persian dates and status (up to 20 most recent); source only.
- [x] Add single compact contract operation modal, Persian date parsing, history paging and computed expiry state/days remaining in the employee dossier; initial 30-day warning, Tehran business date, no live permissions or browser validation.
- [ ] Build cross-employee expiry dashboard, filters, configurable warning policy, approved successor reconciliation and reminder delivery.
- [ ] Implement guarded signed-contract attachment upload/download, independent sensitive-field access and audit.
- [ ] Implement daily expiry scan, durable reminders/outbox and deduplication, catch-up after downtime, assignment and renewal/non-renewal follow-up; reauthorize recipients and resolve superseded reminders.
- [ ] Add configurable warning thresholds (proposed 30/15/7/3/1/0 days), notification and cartable integration; expired/open-ended and approved-successor cases.
- [x] Register employee CQRS and repositories through DI with a fail-closed LockedHrEmployeeAccess adapter, server-side access checks and antiforgery; no live HR data exposed by default.
- [ ] Complete actual Access Catalog adapter, permission-driven navigation and direct/indirect enforcement, approved migration and SQL, build, tests, browser review and pilot.
## H03 — Attendance devices (after priority release)
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
- [ ] Contract amendments and compensation components after H02-C priority release; base contract/expiry work is tracked in H02-C.
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

Evidence: v0.9 contract command, transactional repository, DI and employee dossier modal/list added. Contract read/create/edit/approve/terminate scopes are independent; locked adapter still denies all real HR access. The provisional 30-day warning and Asia/Tehran calendar are assumptions pending company settings. No schema changes in this stage; no new migration required. No build, runtime tests, DB changes, actual notification delivery or main/ai-core changes. v0.8 moves create/edit into responsive Bootstrap modals and adds eleven nullable HR-only fields. Existing personnel/Identity records are preserved; initial migration is unchanged. A separate hand-authored additive migration, Designer, snapshot and review SQL have been committed; no SQL executed. Employment start date, status, department/position and contract/pay data remain separate from personal profile. Actual HR permission adapter, build, browser and migration verification still pending. v0.7 migration artifacts committed on hrm only. Hand-authored because .NET SDK is absent; not scaffolded by EF tooling. RowVersion explicitly required on all six HR entities. Up only creates six HR tables, indexes and constraints; Down drops those tables and their HR data. Review SQL assumes dbo and requires the preceding CRM migration record; not executed. No production migration, DB changes, data backfill, or AccLevel activation. v0.6 employee workspace and create/edit source added; actual HR access intentionally denied by default pending AccLevel integration and schema. Admin role permits preview shell only. Personnel code and account link are immutable in the edit form; existing account names initialized from verified IsEmployee account, later name edits affect HR profile only. Candidate lookup reads separate Identity context without cross-context atomicity; final uniqueness in CRM DB. Contract history read-only, not full contract CRUD. No live DB queries or writes, migrations, build or browser test executed. v0.5 adds contract source foundation and the user-approved priority release H02-C. Contract type is fixed/open-ended for now; expiry policy is deterministic and does not change employment state. No automatic notifications, writes, migrations or endpoints. Contract overlap, renewal authorization, approved-successor reconciliation, policy settings, company timezone and payroll fields remain open. v0.3 five HumanResources Domain entities added and read back from hrm. Per-record guards only; uniqueness, cross-record dates, cycles and permission require persistence/application checks. EmploymentRecord is the source of employment history; no duplicate employment state on EmployeeProfile yet. v0.4 adds per-entity mappings, named DbSets and read-only repository with explicit server-authorized employee ID set (empty set denies); code-level Sepidar configuration exclusion is in place, runtime model isolation remains untested. No DI registration, write repository, migration, device connection, UI, endpoint, database or main changes. Cross-record overlap, cycles and Identity validation remain pending. No checked-out repository or dotnet SDK; build and tests not run.
