# 31. Backward compatibility

**Classification:** Mandatory

## Required behavior
Preserve existing routes, API payloads, permissions, data, integrations and valid behavior unless change is authorized. Trace callers including dynamic JS/Razor/SQL. Plan expand/migrate/contract and rollback when schema evolution is requested.

## Prohibited behavior and exceptions
No silent breaking rename, destructive migration, incompatible shared-helper change or unnecessary fallback masking a bug.

## Examples
Good: update all callers and plan safe migration for an approved rename; bad: rename public field without checking frontend.

## Review checklist
- [ ] Check contract diff, legacy flows, migration/rollback plan and consumer tests.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
