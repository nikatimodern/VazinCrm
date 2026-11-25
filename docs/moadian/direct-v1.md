# Direct Moadian V1 in Development

The authority specification is [RC_TICS.IS_V01](https://hpmgroup.co/wp-content/uploads/2022/09/%D8%B3%D9%86%D8%AF_%D8%AF%D8%B3%D8%AA%D9%88%D8%B1%D8%A7%D9%84%D8%B9%D9%85%D9%84_%D9%81%D9%86%DB%8C_%D9%86%D8%AD%D9%88%D9%87_%D8%A7%D8%AA%D8%B5%D8%A7%D9%84_%D8%A8%D9%87_%D8%B3%D8%A7%D9%85%D8%A7%D9%86%D9%87_%D9%85%D9%88%D8%AF%DB%8C%D8%A7%D9%8614010422.pdf), sections 6 and 7 and appendices 1 and 2. A read-only aggregate of successful Sepidar 2 logs confirmed V1 packet markers, receipt UID and reference number, and the inquiry status field. It did not expose packets, tokens, keys or taxpayer-specific identifiers. It did not establish the current authority Sandbox URL or the accepted wire signature.

| Operation | Direct V1 | Direct V2 |
| --- | --- | --- |
| Original domestic type 1 sale and processing inquiry | Available in the Sandbox flow | Existing flow |
| Export and reference invoices | Disabled | Existing flow |
| Separate work-folder status and payment | Disabled | Existing flow |
| Identity-only connection check | Signed token request and fiscal memory identity | Existing configuration readiness |

## First actual authentication

1. Confirm the authority's V1 Sandbox URL, identity and matching RSA key pair through the approved channel. The documented production prefix is `https://tp.tax.gov.ir/req/`. ERP does not allow that origin. The application allows only HTTPS on `sandboxrc.tax.gov.ir` with a V1 `/req/` base path. This allowlist is not proof that the authority currently exposes V1 there. If the confirmed test prefix differs, review the allowlist with the official source before enabling it.
2. Apply `PinTaxAuthorityProtocol` to the approved development database. Set `Taxation:EnableLocalPreview` and, only with approval, `Taxation:Sandbox:Enabled` and the confirmed non-secret `Taxation:Sandbox:V1BaseAddress`. Leave Production disabled. Restart the host after updating the transport settings.
3. Open the tax memories page as an administrator over HTTPS. Select V1, the Sandbox memory and the matching economic code, and import its RSA private key PEM. The binary key is encrypted by Windows DPAPI for the application account under its LocalAppData. Keep the application on the same protected account and retain a separate approved key backup. For V2 enter the certificate's SHA-256 fingerprint and make the certificate's private key available to the application account in `LocalMachine\My`.
4. Select the V1 connection check. It calls only token acquisition and fiscal information, then checks active memory status and the seller economic code. No invoice is sent by this action. Tokens are not returned to the browser. Separately authorize any first invoice submission.

Each dispatch pins its protocol and a reference to the matching credential. The company has one active sending method. Methods can be changed after open operations are resolved. Reconciliation uses the protocol pinned to the original dispatch. The settings file is local to the Windows application account and host.

## Sepidar cutover

The ERP serial and invoice uniqueness checks cover ERP records. Sepidar has an independent queue and can assign a conflicting serial or send the same business invoice. Prefer a dedicated fiscal memory and a single designated sender for each business invoice. For a shared memory, reconcile earlier Sepidar sends, reserve disjoint serial ranges with the finance team, and agree a single sender or cutover point before enabling any live ERP submission.
