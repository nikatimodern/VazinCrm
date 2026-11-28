# Human Resources Management — draft design v0.1
Status: proposal; not implemented. First new AI-integrated module, fully functional without AI. Device vendor/model, connection protocol, shift policies, payroll authority and effective legal rules remain unresolved.

## Integration and architecture
Reuse Identity and existing employee classification after verifying actual AppUser schema, access catalog and existing personnel/employee records; no duplicate user account. EmployeeProfile is a distinct HR record; optional unique AppUserId supports employees without login. Do not use an account flag as the employment lifecycle. Existing employee-to-Sepidar mapping is evidenced by commit c6d5a4b4; actual key types and mappings require inspection. Follow root Onion rules: Domain owns invariants and repositories; SQL maps/persists; application orchestrates via existing approved CQRS dispatcher; endpoint thin, existing UI/RTL, SignalR only for authorized recipients. No new project/package proposed. No external payroll write without approval; confirm whether Sepidar computes final payroll.
Device adapter interface (conceptual): read device metadata, paged attendance events from cursor, health, capability flags. Vendor SDK/API not assumed. If push-only, implement authenticated inbound adapter; if LAN inaccessible from ERP, assess minimal collector deployment. Read-only pilot, no delete/clock/user sync until separately approved. Store raw events append-only with device identity, source event ID if available, employee device key, source time, timezone/offset, received UTC, direction and import batch. Deduplicate by stable device event ID or documented fallback fingerprint that preserves legitimate punches. Commit import and cursor atomically; replay after outages, quarantine unknown identity/ambiguous timestamps; audit and monitor sync. No biometric templates.
Attendance engine: effective-dated calendar, shift assignments, holiday, grace, breaks, overnight shifts, missing/duplicate punches, leave/mission/overtime approvals. Preserve original events; corrections are separate approved records with actor/reason. Use deterministic processing, versioned rule sets, explicit exceptions, recalculation only for open periods; lock approved monthly snapshots, controlled reopen and audit. No AI calculation of payroll or leave balances.

## Candidate logical entities (review before migration)
- EmployeeProfile (optional unique AppUserId; unique personnel code), EmploymentRecord (history/status/start/end), EmployeeDocument (restricted attachment references), Department (parent optional), JobPosition, EmployeeAssignment (department, position, manager, effective dates).
- AttendanceDevice, EmployeeDeviceIdentity (unique device + external user key with valid dates), AttendanceSyncRun/Cursor, RawAttendanceEvent, AttendanceException, AttendanceCorrection (approval and original reference).
- WorkCalendar + CalendarDay, ShiftDefinition, ShiftAssignment, WorkRuleSet + version, DailyAttendanceResult (employee/workday/rule version), WorkPeriod + MonthlyWorkSnapshot (employee/period, approval and lock).
- LeaveType, LeaveLedgerEntry, LeaveRequest; MissionRequest, OvertimeRequest; reuse existing approval workflow if fit, otherwise explicit RequestApproval with actor/time/reason and transition invariants.
- EmploymentContract + amendments, CompensationComponent, PayrollPeriod + PayrollResult/Line (only if ERP owns calculation), Loan/Advance and repayment schedule, Sepidar mapping/export batch and idempotency reference.
- Later: RecruitmentVacancy/Application, Training/CourseParticipation, PerformanceMetric/Review (human-led), AssetAssignment, OffboardingChecklist.
Relationships and exact key/decimal/time types, audit base, unique indexes, retention and delete restrictions need schema inspection. Preserve immutable history and segregate salary/doc access. No migration yet.

## Authorization and data
Separate permissions for employee profile, attendance, device admin, own requests, approvals, HR documents, contracts, payroll read/write/export, reports, and AI actions. Scope: self, authorized team or explicitly allowed employees; verify real access catalog conventions, do not rely on roles or UI alone. Prevent self-approval where policy requires separation; audit reads/exports of confidential information as appropriate. Prevent data leaks through search/report/file/notification and assistant tools. Separate duty for monthly approval and payroll export. External AI gets only approved minimized non-identifiable data; sensitive HR uses LocalOnly policy after VM readiness. Human review for hiring, discipline, pay and performance decisions.

## Delivery gates
1. Inspect Identity/employee/access catalog, device model/protocol/timezone and sample anonymized logs; review policies and schema.
2. Personnel, organization, permission setup and UI; approved migration + SQL script, test with non-production data.
3. Device adapter and replay-safe import, reconciliation and monitoring.
4. Shift/calendar/rules engine, daily attendance and exception UI; deterministic tests for overnight/holiday, duplicates, missing punches, change history.
5. Requests, approval, self-service, notifications, month close/reopen.
6. Contracts, payroll/Sepidar decision, verified effective-dated legal rules, reconciliation and approval before export.
7. HR dashboard and AI read-only aggregate tools via Vazin AI Core; knowledge Q&A and anomaly explanations with source links, then scoped agents.
8. Recruitment, training, performance, assets, offboarding and reporting; pilot, security/regression tests and release approval.
HRM does not block on local VM or full AI rollout. Cross-branch integration after approved contract baseline via a dedicated integration branch or approved merge; no automatic main merge. No runtime tests performed for this draft.
