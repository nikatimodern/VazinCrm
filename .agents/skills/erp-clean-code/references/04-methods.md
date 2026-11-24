# 04. Methods

**Classification:** Mandatory; size targets recommended

## Required behavior
Give methods one coherent operation, explicit inputs/outputs and predictable side effects. Prefer guard clauses, small helpers and meaningful request models where inputs belong together. Minimize visibility and avoid ambiguous Boolean mode flags.

## Prohibited behavior and exceptions
Do not silently change state from a read, hide side effects, split cohesive logic mechanically, or introduce DTOs merely to meet a metric. Framework signatures and simple Boolean setters are valid.

## Examples
Good: if (!canApprove) return Forbidden(); then approve; bad: deeply nested authorization or Process(true,false).

## Review checklist
- [ ] Check branches, parameters, return semantics, state mutations and null handling.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
