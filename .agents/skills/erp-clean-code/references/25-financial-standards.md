# 25. Financial standards

**Classification:** Critical

## Required behavior
Follow crm-financial and actual approved formulas. Use decimal in backend, explicit currency/unit, precision, rounding stage and discount/tax order. Verify client display matches authoritative server values, payment balance, allocations and reconciliation.

## Prohibited behavior and exceptions
No invented tax/rounding rules, floating-point authoritative amounts, duplicate payment or live financial writes without explicit approval.

## Examples
Good: test fractional tax at approved rounding boundary and allocation remainder; bad: change floor to round for convenience.

## Review checklist
- [ ] Verify invoice totals, partial payments, zero/negative cases, tax exemptions and external reconciliation.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
