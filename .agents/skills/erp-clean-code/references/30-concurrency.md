# 30. Concurrency and idempotency

**Classification:** Critical for sensitive writes

## Required behavior
Use server-side idempotency/unique keys, concurrency tokens or appropriate isolation according to business semantics. Define retry and conflict behavior; handle uncertain external outcomes with reconciliation. Disable repeated UI submissions as UX only.

## Prohibited behavior and exceptions
No check-then-insert race without protection, blind financial retries, shared concurrent DbContext or client-only deduplication.

## Examples
Good: enforce a unique transfer key and reconcile ambiguous timeout; bad: trust a disabled button to prevent duplicate invoices.

## Review checklist
- [ ] Test concurrent requests, retry after timeout, optimistic conflict, rollback and duplicate detection.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
