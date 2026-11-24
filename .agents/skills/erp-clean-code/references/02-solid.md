# 02. SOLID

**Classification:** Mandatory with justified exceptions

## Required behavior
Keep one reason to change per class (SRP); extend stable behavior at real variation points (OCP); preserve contracts of substitutes (LSP); expose narrow interfaces (ISP); depend on Domain contracts rather than infrastructure (DIP). Prefer composition.

## Prohibited behavior and exceptions
Avoid God classes, speculative interfaces, inheritance that breaks contracts, and dependency cycles. Simple concrete classes are valid when no boundary requires abstraction.

## Examples
Good: handler depends on an existing Domain repository interface; bad: handler constructs a SQL repository or throws for valid inherited operations.

## Review checklist
- [ ] Check responsibilities, contract substitution, interface consumers and project dependencies.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
