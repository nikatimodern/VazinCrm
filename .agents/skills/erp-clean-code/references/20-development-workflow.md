# 20. Development workflow

**Classification:** Mandatory

## Required behavior
Before: inspect instructions, relevant code, dependencies, current behavior and scope. During: reuse, apply appropriate layers, preserve compatibility and avoid unnecessary changes. After: inspect diff, run relevant checks, report failures/untested areas in Persian.

## Prohibited behavior and exceptions
No blind edits, fabricated verification, unrelated refactors or unauthorized live writes. Read-only reviews remain read-only.

## Examples
Good: trace all indirect permission routes before modifying a handler; bad: change UI only and claim authorization fixed.

## Review checklist
- [ ] Complete before/after checklists; record changed files, commands, residual risks and approval needs.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
