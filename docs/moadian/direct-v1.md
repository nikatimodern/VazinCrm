# Direct Moadian V1 in Development

The authority specification is [RC_TICS.IS_V01](https://hpmgroup.co/wp-content/uploads/2022/09/%D8%B3%D9%86%D8%AF_%D8%AF%D8%B3%D8%AA%D9%88%D8%B1%D8%A7%D9%84%D8%B9%D9%85%D9%84_%D9%81%D9%86%DB%8C_%D9%86%D8%AD%D9%88%D9%87_%D8%A7%D8%AA%D8%B5%D8%A7%D9%84_%D8%A8%D9%87_%D8%B3%D8%A7%D9%85%D8%A7%D9%86%D9%87_%D9%85%D9%88%D8%AF%DB%8C%D8%A7%D9%8614010422.pdf), sections 6 and 7 and appendices 1 and 2. A read-only aggregate of successful Sepidar 2 logs confirmed V1 packet markers, receipt UID and reference number, and the inquiry status field. It did not expose packets, tokens, keys or taxpayer-specific identifiers. It did not establish the current authority Sandbox URL or the accepted wire signature.

| Operation | Direct V1 | Direct V2 |
| --- | --- | --- |
| Original domestic type 1 sale and processing inquiry | Available in the Sandbox flow | Existing flow |
| Export and reference invoices | Disabled | Existing flow |
| Separate work-folder status and payment | Disabled | Existing flow |
| Identity-only connection check | Signed token request and fiscal memory identity | Existing configuration readiness |

## First actual authentication

1. Confirm the authority's V1 Sandbox URL, identity and matching RSA key pair through the approved channel. The Sandbox transport allows only HTTPS on `sandboxrc.tax.gov.ir` with a V1 `/req/` base path. This allowlist is not proof that the authority currently exposes V1 there. If the confirmed test prefix differs, review the allowlist with the official source before enabling it.
2. Apply `PinTaxAuthorityProtocol` and `ManageTaxMemorySettings` to the approved development database. Sign in as an administrator to the Development ERP. An explicit deployment-level disable of the tax module still takes precedence.
3. On the tax memories page, create or edit a Sandbox memory, select V1 and enter the seller economic code. Import both matching RSA PEM files over HTTPS. The private key is encrypted with Windows DPAPI under the application account and kept outside the project; SQL stores the fingerprint. V2 administrators can select a certificate in `LocalMachine\My` or install a PFX/P12 over HTTPS if the application account has write permission to that store. Keep older signing credentials that are referenced by queued documents.
4. After the approved Sandbox destination is confirmed, enable Sandbox communication for that memory and select the connection test. The check reports local settings, credentials, HTTPS, authentication and server information. It sends no invoice or payment and does not display tokens. Separately authorize the first actual test or submission.

Each dispatch pins its protocol and a reference to the matching credential. The company has one active sending method. Methods can be changed after open operations are resolved. Reconciliation uses the protocol pinned to the original dispatch. Managed memory metadata and audit events are stored in CRM SQL. The legacy connection file and existing deployment configuration remain fallback sources for unconfigured memories.

In Development, an administrator can save a production memory and run the production connection check from its card. The check uses a separate transport restricted to the authority's production authentication and information endpoints. Production memory activation and submission are disabled.

## Sepidar cutover

The ERP serial and invoice uniqueness checks cover ERP records. Sepidar has an independent queue and can assign a conflicting serial or send the same business invoice. Prefer a dedicated fiscal memory and a single designated sender for each business invoice. For a shared memory, reconcile earlier Sepidar sends, reserve disjoint serial ranges with the finance team, and agree a single sender or cutover point before enabling any live ERP submission.
