# 26. API standards

**Classification:** Mandatory

## Required behavior
Keep HTTP endpoints thin, bind explicit request DTOs, validate and authorize, preserve established envelopes and cookie-expiry behavior. Use appropriate HTTP methods/statuses, stable pagination and clear error contracts. Version breaking changes with approval.

## Prohibited behavior and exceptions
No state mutation through GET, overposting entity objects, sensitive error payloads, unbounded lists or contract changes without caller review.

## Examples
Good: POST with validated command and authorization; bad: GET /Delete?id=... as a mutation.

## Review checklist
- [ ] Check clients, binding, 400/401/403/404/409 semantics where compatible, antiforgery and integration tests.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
