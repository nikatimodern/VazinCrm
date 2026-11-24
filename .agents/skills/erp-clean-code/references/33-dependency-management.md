# 33. Dependency management

**Classification:** Mandatory

## Required behavior
Prefer existing packages and native APIs. Inspect transitive dependencies, supported versions, licenses, security advisories, lockfiles and compatibility before requesting approval for additions/upgrades. Use reproducible restores and scoped dependency changes.

## Prohibited behavior and exceptions
No unapproved packages/frameworks, arbitrary CDN changes, ignored critical vulnerabilities or broad updates unrelated to the task.

## Examples
Good: reuse existing HTTP client; bad: add a second library for identical requests.

## Review checklist
- [ ] Check actual manifests, restore/build, vulnerability scan when available, licenses and compatibility.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
