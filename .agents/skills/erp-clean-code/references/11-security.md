# 11. Security and authorization

**Classification:** Critical

## Required behavior
Deny by default; enforce current permission and record Scope server-side for every read/write and direct/indirect path, including search, export, report, print, files, AJAX, APIs and real-time events. Parameterize SQL; validate input and output encode; apply antiforgery where cookie-authenticated writes require it; protect secrets and audit sensitive changes.

## Prohibited behavior and exceptions
Never rely on hidden controls or supplied IDs, bypass permission checks, interpolate untrusted SQL, expose secrets or log sensitive payloads. Whitelist dynamic SQL identifiers; stored procedures must also parameterize dynamic SQL.

## Examples
Good: filter the query by authorized Scope before projection and use LINQ parameters; bad: fetch by user-supplied ID without record permission or concatenate SQL.

## Review checklist
- [ ] Trace caller-to-data paths; test unauthorized role, scope, indirect route, ID tampering, XSS/CSRF and export isolation.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
