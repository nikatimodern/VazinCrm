# 21. ERP-specific standards

**Classification:** Mandatory

## Required behavior
Use actual ERP terminology, current access registry/Scope, shared site.js helpers and existing modal/components. Preserve Persian RTL, financial formulas, configured integrations and current versioning. Check backend, frontend and SQL dependencies for entity changes.

## Prohibited behavior and exceptions
No hardcoded role alternatives that bypass current access system, parallel helpers, invented tax rates, unrequested release/version changes or automatic deletion of external mappings outside specialist-approved exceptions.

## Examples
Good: verify current Person entity and callers before renaming; bad: assume old Customer schema.

## Review checklist
- [ ] Load crm-backend, frontend, financial, EF or Sepidar specialist skills as applicable; verify current source.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
