# 15. Git and change management

**Classification:** Mandatory

## Required behavior
Keep scoped diffs, meaningful commits, inspect current branch and existing changes, preserve user work. Use requested branch; review diff, build/test where possible, record DB impact. Commit/push/merge/PR only when authorized.

## Prohibited behavior and exceptions
No force-push/reset, unrelated formatting, secret commits or silent merge. Do not invent version bumps or change public contracts without request.

## Examples
Good: a focused fix commit on the requested branch; bad: reformat all files while fixing one bug.

## Review checklist
- [ ] Verify branch/base, changed paths, secret scan, status and merge readiness.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
