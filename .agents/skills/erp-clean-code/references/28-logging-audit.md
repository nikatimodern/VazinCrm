# 28. Logging and audit

**Classification:** Mandatory; confidentiality Critical

## Required behavior
Use structured ILogger logs with useful event context/correlation and safe identifiers; audit sensitive state transitions with actor, action, entity, timestamp and relevant outcome. Follow current audit conventions and restrict access/retention.

## Prohibited behavior and exceptions
Never log credentials, OTPs, connection strings, raw personal or financial payloads; avoid duplicate logs and silent audit loss.

## Examples
Good: log operation ID and safe failure category; bad: log complete request including token.

## Review checklist
- [ ] Check audit completeness, access, retention, safe redaction and error correlation.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
