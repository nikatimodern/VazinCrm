# 10. Frontend

**Classification:** Mandatory

## Required behavior
Follow crm-frontend, crm-frontend-reusables and UI skills. Prefer native JS, const/let, scoped CSS, safe text rendering, proper event lifecycle, shared request/feedback helpers, loading/error/empty states and RTL/mobile. Keep validation authoritative on server.

## Prohibited behavior and exceptions
No new jQuery application code or Select2; preserve existing plugin dependencies per specialist skill. No untrusted innerHTML, duplicate listeners, repeated submissions or global pollution.

## Examples
Good: element.textContent = label; bad: element.innerHTML = untrustedLabel.

## Review checklist
- [ ] Check repeated modal opens, keyboard, mobile, disposal, request failures, XSS and shared helpers.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
