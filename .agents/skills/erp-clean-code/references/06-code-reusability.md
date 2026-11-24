# 06. Code reuse

**Classification:** Mandatory

## Required behavior
Search existing helpers, services, components, partials and modals before adding equivalents. Centralize identical business rules. Extend shared contracts only after checking callers; keep feature-only logic local.

## Prohibited behavior and exceptions
No duplicate helper, parallel permission service, copied modal or gratuitous generic wrapper. Do not merge unrelated concepts solely because code resembles each other.

## Examples
Good: reuse sendApiRequest and compatible shared modal; bad: clone both into each page.

## Review checklist
- [ ] Search by behavior and name; check caller compatibility and update shared utility references.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
