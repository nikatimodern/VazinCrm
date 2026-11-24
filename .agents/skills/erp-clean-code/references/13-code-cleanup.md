# 13. Code cleanup

**Classification:** Mandatory

## Required behavior
Remove proven unused code, usings, assets, dependencies and obsolete routes within scope; replace magic business values with named constants/enums. Inspect reflection, DI, Razor, JavaScript, SQL/SPs and external callers first.

## Prohibited behavior and exceptions
Never delete based only on IDE usage count; no global cleanup, speculative compatibility shims or removal of externally consumed contracts without approval.

## Examples
Good: remove a private helper after checking all uses; bad: delete a controller action because C# has no references.

## Review checklist
- [ ] Search static/dynamic references, validate builds and affected flows, document removed public contracts.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
