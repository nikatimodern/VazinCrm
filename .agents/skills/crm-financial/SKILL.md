---
name: crm-financial
description: Review and implement Vazin CRM prefactor, supply-factor, cash, tax, discount, payment, and financial workflow logic with precise arithmetic and safe state transitions.
---

# CRM Financial Workflows

Use for monetary calculations, prefactors, supply invoices, cash, approvals, and financial state transitions.

## Procedure
1. Locate current entities, services, enums, calculations, and tests. Trace whether totals are stored, derived, or recomputed.
2. Identify currency/unit, rounding rules, decimal precision, discount/tax order, and any official/unofficial distinctions from actual code.
3. For prefactors, inspect `PreFactorService` and related entity fields before modifying totals. Existing code computes `SumTotalAmountTax = SumTotalAmount + TotalTax - Discount`; preserve current semantics unless explicitly asked to change them.
4. Verify payment, prepayment, received amount, and outstanding balance behavior without double-counting.
5. Check approval roles, state transitions, audit history, duplicate submission, and cross-system consistency.
6. Add or propose edge-case tests for zero values, discounts, rounding, repeated requests, and partial payments.
7. Explain business-rule changes and reconciliation risks in Persian.

## Guardrails
Never invent tax rates, accounting entries, business rules, or currency conversions. Never alter financial formulas or perform live financial writes without explicit approval.

## Project-wide architecture and change policy
- Place application modules directly under the `CRM.Core.ApplicationServices` project root (for example, `Reports/Queries`, `Reports/Commands`, `Reports/Handlers`, and `Reports/Dtos` as needed). Do not introduce a `Features` wrapper folder. Match namespaces to these paths (for example, `CRM.Core.ApplicationServices.Reports.Queries`); keep shared CQRS contracts in `Abstractions/Cqrs`.
- For any new feature that persists data, define its repository interface in `CRM.Core.Domain` and implementation in `CRM.Infrastructures.Sql`; avoid direct DbContext access from new controllers or handlers.
- For new features, consider and suggest CQRS where beneficial, but use the existing `CRM.Core.ApplicationServices` layer; suggest CQRS where beneficial and obtain approval before introducing new CQRS patterns or dependencies.
- Improve code within the requested scope. For broader refactoring, propose affected files, benefits, risks, and a verification plan; wait for explicit approval before proceeding.
