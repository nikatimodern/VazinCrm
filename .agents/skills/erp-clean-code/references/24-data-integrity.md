# 24. Data integrity

**Classification:** Critical

## Required behavior
Enforce business invariants at appropriate domain and persistence boundaries; use constraints, atomic writes, explicit state transitions, audit and reconciliation. Preserve existing records and verify counts before/after approved backfills.

## Prohibited behavior and exceptions
No orphan rows, silent truncation, partial dependent writes or destructive correction without approval. Distinguish valid legacy exceptions from corruption.

## Examples
Good: unique constraint for a true business key and transactional dependent inserts; bad: delete duplicates blindly.

## Review checklist
- [ ] Check keys, foreign keys, precision, state transitions, rollback and backfill reconciliation.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
