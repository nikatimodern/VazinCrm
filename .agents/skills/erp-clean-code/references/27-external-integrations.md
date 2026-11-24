# 27. External integrations

**Classification:** Critical

## Required behavior
Use configured provider/target, verify mapping and identifiers, timeouts, cancellation, retry safety and reconciliation. Follow sepidar-integration for missing records, pending reservations and explicit temporary exceptions. Distinguish CRM/external IDs and tax memory from accounting target.

## Prohibited behavior and exceptions
No guessed schema, duplicate exports, blind retry of writes, deletion during reads outside existing approved exception, or live external writes without approval.

## Examples
Good: verify destination and existing transfer key before posting; bad: resend an invoice after ambiguous timeout without reconciliation.

## Review checklist
- [ ] Check target selection, required mappings, partial failure, idempotency, logs and sandbox/test isolation.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
