# RequestAmount, Payment, BankAccount, Checkbook, Check access rollout

Branch: AccLevel. SQL registration: RegisterRequestPaymentBankCheckAccessCatalog.sql. UserConnection / UserContextDb only. Preview by default (@Apply=0); 5 entities, 26 permissions, 118 proposed initial role grants. Existing grants and explicit overrides are preserved. Matching data migration: 20261125000100_RegisterRequestPaymentBankCheckAccess. Preserve these for the final AccessCatalog_CompleteSetup.sql.

## Source changes
- Shared document scope evaluator, registration/admin/history support for Own, All, SelectedUsers and AllExceptSelectedUsers; these five entities currently use creator ownership.
- RequestAmount: View/Create/Edit/Delete, Approve, ChangeStatus, ChangeDocType, Pay. Check linked order heads and supply factors; scope list/search/counts and payment lists. Restrict aggregate supplier debt to full viewers. Guard approval and payment stage transitions and existing links.
- Payment: View/Create/Edit/Delete; protect both request-payment and supply-payment routes, account links, attachments and dashboard totals. Request-linked payments cannot be edited through legacy supply-payment route.
- BankAccount: View/Create/Edit/Delete, ChangeActive; filter table and account picker and protect settlement reference changes. Legacy finance role restrictions remain additional gates.
- Checkbook: View/Create/Edit/Delete. Edit reserved (no existing edit endpoint); do not add a business operation merely to use a catalog key. Checkbook deletion checks permissions over affected issued checks.
- Check: View/Create/Edit/Delete, ChangeStatus; protect list, detail, create/edit, deletion/status and parent selection. Received checks are Accountancy-backed, unlike issued checks; check received-page and cartable view access in addition to Accountancy access.
- Updated main Razor cards and JS actions, menu view filtering and AccessLive state/form locks.

## Follow-up source changes
- Issued-check and bank-account tables return individual row action grants; edit, delete, status and activation controls respect those grants. Check endpoints validate the parent checkbook and bank account.
- Request payment selection and row actions respect RequestAmount.Pay and Payment grants, including every linked request. Cross-route takeover/deletion of legacy supply payments is rejected. Approval controls and shared bank account modal are permission-aware.
- Recycle bin payment and request records are scoped; restore also requires the record edit grant.
- Transfer list/count and save/detail/delete/status operations check linked bank accounts, including existing and proposed references. Cash and cashier save operations validate bank account references.
- Received-check cartable lists apply Check.View scope in addition to the existing Accountancy controls. Bank picker cache is invalidated on live access updates.
- All five enforcement flags are enabled in appsettings.Development.json only. Production configuration was not modified. No new entity registration or change to the existing SQL registration files.

## Remaining verification and work
- Before launching Development, use an isolated test database, back up UserConnection, preview the registration script (@Apply=0), review role grants, and apply the approved registration (@Apply=1) or the corresponding migration.
- Build with `dotnet build VazinProject.sln`. JavaScript syntax was checked for check.js, requestAmount.js, Layout.js and the settings page inline scripts; C# compilation, database and browser regression tests were not executed in the editing environment.
- Verify the four scope modes, explicit user denies, initial role grants, live revocation across tabs/modals, all workflow transitions, linked records, attachment ownership and deletion/restoration, bank ownership rules, search/count isolation and partial/grouped payment flows.
- Continue the indirect-path audit for cash/cashier reads, aggregates/exports, nested objects and generic Accountancy received-check operations; recheck warehouse, external mappings and reports as their entity rollouts proceed. Do not treat the current source changes as an exhaustive security or runtime verification.

Transfer/cash follow-up: cashier targets are scoped on request lists and related payment operations; previous bank references and payment creator are checked on update. See Transfer-Cash-Access-Rollout.md for remaining integration checks.
