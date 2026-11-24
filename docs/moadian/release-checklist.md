# Moadian implementation and release checklist

## Mission status

| Mission | Scope | Status | Commit |
| --- | --- | --- | --- |
| 1 | Correction, cancellation and return | Implemented and verified with Mock. Official acceptance awaits Sandbox. | `942039ed` |
| 2 | Export and supported invoice patterns | Domestic type 1 and export type 1 are implemented. Test export acceptance with approved customs data in Sandbox. | `46b9a4f1` |
| 3 | Cash, credit and mixed settlement | Implemented and verified with SQL and Mock. Sandbox results remain pending. | `b786fe33` |
| 4 | Tax payment registration | Implemented and verified with SQL and Mock. Ambiguous payment POSTs require manual follow-up. Sandbox results remain pending. | `d86d9ae7` |
| 5 | Integrated SQL and upgrade tests | Verified on dedicated LocalDB, including prior receipts and Data Protection key ring reuse. | `bb4979f1` |
| 6 | Invoice workspace | Implemented. Razor, filters, paging and permissions verified through local HTTP and SQL. | `fecb982d` |
| 7 | Preview, queue, manual submit, inquiry and history | Implemented and verified with local HTTP, Mock and SQL. Official receipts and work-folder status await Sandbox. | `fda0529b` |
| 8 | Batch queue, fiscal memories and product mappings | Durable batch queue for domestic originals, independent outcomes and resumption by reselecting the same invoices. External processing is per document. | `c6356ea4` |
| 9 | Dashboard, filters and Excel | Implemented and verified with SQL and workbook round-trip tests. | `3974087e` |
| 10 | End-to-end verification | Local HTTP, compiled Razor, signed/encrypted Mock exchange, isolated SQL, and host restart verified. | `98184459` |
| 11 | Security and access | Current admin gate, antiforgery, audit and payload redaction verified. Fine-grained permission and record-scope integration depends on AccLevel. | `aa61e0c7` |
| 12 | Release checklist and final verification | Verification results, deployment procedure and remaining dependencies are recorded below. | This document's commit |

Automatic invoice submission during ERP approval, scheduled submission and the automatic-send setting remain deferred. Additional tax patterns require supported source data and verified official rules.

## Database upgrade

Target context is `CRMContextDb`. The tax migrations in deployment order are:

| Migration | Effect |
| --- | --- |
| `20261124061447_AddTaxationFoundation` | Fiscal memories, product mappings and protected invoice snapshots |
| `20261124064618_AddTaxDispatchQueue` | Durable invoice dispatch queue and state and uniqueness constraints |
| `20261124070524_AddTaxSubmissionEvidence` | Pinned submission identity and protected authority evidence |
| `20261124090627_SupportTaxReferenceDispatches` | Reference links, allowed document kinds and uniqueness indexes |
| `20261124095706_AddTaxPaymentDispatches` | Payment requests, amount and outcome constraints and deduplication |
| `20261124103023_AddTaxDispatchEvents` | Append-only send and inquiry history |

The forward tax delta adds schema and replaces queue constraints without deleting rows. Generate the idempotent script from the actual last applied migration. The preceding CRM migration on this branch is `20261119220500_AddQualityControlCustomerNotes`. Compare the target database's migration history, review the SQL, and validate it on a restored staging copy. Schedule the upgrade under the release process after a database backup. Keep rollback planning and backup restoration separate from EF Down scripts, which remove the schema introduced by their migrations.

Use a SQL session with filtered-index SET options enabled. With the verified ODBC `sqlcmd` client, pass `-I` for `QUOTED_IDENTIFIER ON` and `-b` to return a nonzero exit status on SQL errors.

```powershell
$sqlProject = '02.Infrastructures/CRM.Infrastructures.Sql/Generics.Infrastructures.Sql'
$webProject = '03.EndPoints/CRM.EndPoint.FrontEnd'
dotnet ef migrations script 20261119220500_AddQualityControlCustomerNotes 20261124103023_AddTaxDispatchEvents `
    --idempotent --project $sqlProject --startup-project $webProject --context CRMContextDb --output tax-upgrade.sql
```

No production database was accessed or upgraded for this work.

## Repeatable test execution

Use a dedicated Windows SQL Server LocalDB instance named `VazinTaxationStage2Tests`. The opt-in SQL tests create and remove uniquely named databases on that instance. The restart test restarts the instance. Run a single SQL suite at a time on it. The test host binds a loopback port, supplies test identities, registers an in-process Mock authority, and uses synthetic data and test certificates.

```powershell
dotnet build VazinProject.sln
$env:VAZIN_TAX_LOCALDB_TESTS = '1'
dotnet test VazinProject.sln --no-build
python scripts/check-onion-architecture.py
```

The end-to-end suite covers the compiled Razor views and HTTP handlers, role denial, antiforgery, payload signing and encryption, receipt and work-folder status, payments, duplicate batch rows, and restarting the application before recovering an ambiguous cancellation. SQL tests cover migrations with existing records, active-memory races, unique serials, mapping changes, immutable snapshots, stale versions, queue concurrency, receipt-write failure and duplicate prevention.

### Final verification

- Clean solution rebuild succeeded with zero errors and 256 warnings. The archived GitHub `main` revision `13fb738157d413ddffdde589c7dc1391d68139e4` produced the same 256 warnings. All 109 unique file, code and message signatures matched.
- The complete solution test run passed 122 tests, including 22 SQL Server tests. No tests failed or were skipped.
- EF confirmed that the runtime model matches the latest migration snapshot. The forward idempotent SQL applied all six tax migrations to a disposable LocalDB database. A second execution preserved the seeded fiscal-memory row and migration history.
- The architecture reference checker and JavaScript syntax checks passed.

## Sandbox preparation and controlled validation

1. Obtain the organization's approved Sandbox seller and fiscal memory identity, the signing certificate for that memory, and approved synthetic buyer, goods/service and unit codes. Keep a domestic, rial, type 1 sales invoice ready with a unique fiscal serial and a verified issue time and settlement method. Keep ERP amounts intact.
2. Use a separate Development application and test database. Keep Production transport unavailable. Configure `Taxation:EnableLocalPreview` for local preview and queue. Leave `Taxation:Sandbox:Enabled` off until live Sandbox work is separately authorized.
3. Provide `Taxation:Sandbox:BaseAddress` with the official, allowlisted Sandbox origin and v2 path used by the application. Configure `Taxation:Sandbox:Memories:<memory-guid>:CertificateSha256` and `SellerEconomicCode` in the approved deployment configuration. Do not put private keys or secrets in source control. The signing certificate and private key must be available through Windows `LocalMachine\My` to the application account. Verify the SHA-256 certificate hash, validity, RSA key availability and account access on the intended host.
4. Preserve the existing ASP.NET Core Data Protection key ring, application discriminator, purpose and application-account access. Back up the key ring with access controls and encryption. Verify that a snapshot written before a process restart opens after the restart on the deployment host. Use isolated key rings between environments.
5. After authorization for Sandbox connectivity, verify TLS, nonce, authentication, and server encryption keys without submitting an invoice. Obtain explicit approval before the first real invoice POST. Submit one approved original through the manual queue. Preserve the attempt and receipt references, and verify processing and work-folder status independently. If the POST result is ambiguous, perform inquiry for the same attempt and preserve the original attempt identity.

The real Sandbox steps are pending. Authority onboarding, functional certificate access, and the official response contract have been exercised only with synthetic test material where applicable. Independent Sandbox identity provisioning and real authorization must come from the organization's approved channel.

## Production gates and known limitations

- Complete the [AccLevel integration](access-catalog-integration.md), including document and accounting scopes for direct IDs, batch items, reports and exports.
- Verify key-ring restore, signing certificate access and renewal, and isolate Sandbox identity, configuration, keys and data from operational resources. Keep the product's Production boundary disabled until a separate release approves a Production transport and deployment policy.
- Verify log forwarding and retention for event 6201. Keep request bodies, authorization headers, private keys, tokens and protected data out of diagnostic logs.
- The invoice and payment queues have unique database constraints. Reports and lists page in SQL, but text search and grouped status counts require representative-volume query plan and load testing. The Excel output limits a filtered run to 5,000 rows.
- The dashboard marks known prerequisite gaps as missing data and sends other unqueued records to preview validation. Financial validation still runs through the preview and payload contracts.
- Batch selection supports domestic originals. Export filings require their per-document customs data. Payment outcomes without a definitive response remain for manual follow-up under the currently verified contract.
- Send and inquiry history is paged in groups of 50 using the event ID. Historical rows retain their pre-history receipt and current status fields.
- Tax units come from the [official RC_UMGS.ST_V1.18 list](https://www.intamedia.ir/The-regulation-of-store-terminals/articleType/ArticleView/ID/17268). Each product and ERP sales unit has a separate mapping. Select only an equivalent tax unit. For example, kilogram and gram have distinct codes (`164` and `1622`). The mapping does not convert quantities or unit prices. Existing invalid codes are flagged for review and preserved until an operator replaces them.
- Interactive browser viewport testing and live authority response checks remain outside the local headless HTTP assertions.
