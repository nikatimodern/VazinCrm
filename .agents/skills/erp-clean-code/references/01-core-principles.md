# 01. Core principles

**Classification:** Mandatory

## Required behavior
Apply DRY to duplicated business knowledge, KISS, YAGNI, separation of concerns and single source of truth. Fail early on invalid inputs; use least privilege. Improve touched code without unrelated rewrites.

## Prohibited behavior and exceptions
Do not duplicate tax or permission rules, build speculative abstractions, or refactor unrelated modules. Similar-looking code with different business semantics may remain separate.

## Examples
Good: reuse the existing amount calculator; bad: copy its formula into a new controller.

## Review checklist
- [ ] Verify one owner per business rule; inspect existing utilities and callers; ensure the solution is the smallest correct change.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
