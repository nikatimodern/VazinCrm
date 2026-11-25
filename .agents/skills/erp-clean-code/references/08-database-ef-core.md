# 08. Database and EF Core

**Classification:** Mandatory

## Required behavior
Follow efcore-sqlserver. Verify exact schema/context, named DbSets and per-entity Fluent API configurations. Use server-side filters, projections, AsNoTracking for suitable reads, stable paging, parameterized SQL and appropriate transactions/constraints. Measure query plans before indexing.

## Prohibited behavior and exceptions
Avoid N+1, queries in loops, early ToList, arbitrary untranslatable C# in LINQ, unbounded reads and raw user-input concatenation. Migration generation and live SQL changes require explicit approval.

## Examples
Good: Where(...).Select(...).ToListAsync(ct); bad: load all rows then filter locally.

## Review checklist
- [ ] Verify translation, query count, ordering, cardinality, indexes, foreign keys, precision and transaction boundaries.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
