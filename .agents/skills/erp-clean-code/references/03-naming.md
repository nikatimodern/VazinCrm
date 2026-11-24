# 03. Naming

**Classification:** Mandatory

## Required behavior
Use descriptive English identifiers; PascalCase types/public members, camelCase locals/parameters, existing private-field convention, Async suffix for asynchronous methods, Is/Has/Can for booleans where natural. Keep domain terminology consistent.

## Prohibited behavior and exceptions
Avoid x/data/flag for meaningful business values, unclear abbreviations and misleading names. Preserve established external API/SQL names unless migration is approved; short loop indices are fine.

## Examples
Good: GetPersonAsync, totalAmount, isApproved; bad: GetData2, tmp, flag.

## Review checklist
- [ ] Review names, units, namespaces, filenames and all references when renaming.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
