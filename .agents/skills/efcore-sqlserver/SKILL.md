---
name: efcore-sqlserver
description: Diagnose, implement, and optimize EF Core LINQ queries, entity relationships, SQL Server mappings, transactions, and migration plans in Vazin CRM.
---

# EF Core and SQL Server

Use for queries, schema mapping, performance, concurrency, and persistence bugs.

## Procedure
1. Identify the exact DbContext and entity mapping (`CRMContextDb` or the appropriate Sepidar context).
2. Keep repository interfaces in `CRM.Core.Domain`, SQL implementations and Fluent API in `CRM.Infrastructures.Sql`; never introduce EF Core dependencies into Domain or the existing ApplicationServices layer. Inspect entity properties, navigation mappings, keys, indexes, and existing migrations before proposing changes.
3. Keep filtering/projection server-side. Verify EF translation; do not embed arbitrary C# string normalization or `int.Parse` in provider queries.
4. For reads, prefer `AsNoTracking()` and targeted `Select`; use `Include` only when entity graph materialization is necessary.
5. Check cardinality, duplicate rows, nullability, N+1 patterns, pagination, and query count.
6. For writes, examine concurrency, idempotency, transaction boundaries, and failure recovery.
7. Verify with a safe query/build/test when possible and explain the generated SQL or query behavior if relevant.

## Guardrails
Never execute SQL writes, migrations, or schema changes without explicit authorization. Do not assume CRM and Sepidar share an atomic transaction. Never guess external table columns.

## Project-wide architecture and change policy
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For new features, consider and suggest CQRS where beneficial, but use the existing `CRM.Core.ApplicationServices` layer; suggest CQRS where beneficial and obtain approval before introducing new CQRS patterns or dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
