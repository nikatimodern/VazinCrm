# 16. Quality metrics

**Classification:** Recommended except zero new build errors and critical defects

## Required behavior
Use metrics as signals: methods ~10–30 lines, classes preferably <300 lines, ~3 parameters, nesting 2–3, cyclomatic <=10, cognitive <=15, coverage goal 80%, duplication <3%. Define baseline and measure scope. No new build errors or known critical security defects.

## Prohibited behavior and exceptions
Do not sacrifice clarity, add wrappers, alter behavior or block legacy code solely for arbitrary thresholds. Generated code may be excluded with documented justification.

## Examples
Good: extract a coherent complex validation; bad: split every 31-line method into meaningless fragments.

## Review checklist
- [ ] Record measurement/tool, baseline, changed-code delta and justified deviations.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
