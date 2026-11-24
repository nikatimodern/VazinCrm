# 12. Comments and documentation

**Classification:** Mandatory

## Required behavior
Write clear English identifiers/comments, document why a non-obvious business invariant or workaround exists, update comments with behavior. Use XML documentation for public contracts when useful; preserve Persian UI text.

## Prohibited behavior and exceptions
No stale comments, commented-out dead code, trivial narration or TODO without actionable context. Explain unusual legacy constraints rather than hide them.

## Examples
Good: // Round down tax to match the approved invoice rule. Bad: // Add one before i++.

## Review checklist
- [ ] Review comment accuracy, business rationale and documentation changes.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
