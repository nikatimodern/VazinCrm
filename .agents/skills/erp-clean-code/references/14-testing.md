# 14. Testing

**Classification:** Mandatory

## Required behavior
Test changed business rules, edge cases, invalid input, authorization and regressions. Use existing test style and isolated fixtures; use relational integration tests for EF translation/constraints when possible. Run relevant build/test/lint checks; report exact commands and outcomes.

## Prohibited behavior and exceptions
Do not invent passing results, weaken assertions, run tests against live financial systems or force new test dependencies without approval. Manual verification is documented when automation is unavailable.

## Examples
Good: test own/other record Scope and rounding boundary; bad: assert only HTTP 200 for a sensitive operation.

## Review checklist
- [ ] Check positive/negative/boundary cases, test isolation, regression coverage and actual command results.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
