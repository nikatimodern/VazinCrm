# Architecture review

- [ ] Read rule 34 and crm-backend; inspect actual projects and references.
- [ ] Check dependency direction, cycles, Domain purity and Framework independence.
- [ ] Verify repository contracts in Domain, SQL implementations, no new DbContext in controllers/handlers, and DI composition/lifetimes.
- [ ] Verify application use cases, CQRS dispatcher, vertical slice files, request/result mapping and server-side scope.
- [ ] Verify query/command semantics, transaction boundaries, external adapters, tests and compatibility.
- [ ] Run the project-reference checker and relevant build/tests; record actual results and legacy exceptions. Source-level review remains necessary.
