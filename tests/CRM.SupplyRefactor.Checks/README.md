# Supply refactor regression checks

Run from the repository root:

```powershell
dotnet run --project tests/CRM.SupplyRefactor.Checks --configuration SupplyUi
```

No test packages, external services, real database connections, schema changes or writes are required. Checks cover the public action/authorization contract, dependency direction, CQRS registrations, pure allocation rules, command orchestration and cancellation. EF SQL checks intercept both opening a connection and reading commands; fake readers return only fixture data.

Payment checks cover decimal amounts, invoice links, actor/date metadata, attachment ownership, audit sequencing and rollback on failure. EF model comparison verifies that no schema migration is required. Stored-procedure implementations are not executed by this offline runner.
