# 07. Exception handling

**Classification:** Mandatory

## Required behavior
Validate expected business failures with the existing result/HTTP conventions. Catch specific exceptions only to handle, translate or add useful context; preserve stack with throw; propagate cancellation appropriately. Centralize unexpected error mapping and use structured logs without secrets.

## Prohibited behavior and exceptions
No empty catch, swallowed failure, leaked stack trace, exception-driven normal flow or blind retry. Avoid logging the same exception at every layer.

## Examples
Good: catch DbUpdateException where recovery is possible, otherwise let the central handler log it; bad: catch { return success; }.

## Review checklist
- [ ] Check failure paths, rollback, cancellation, useful safe user messages and logs.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
