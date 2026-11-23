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

## Remaining verification and work
- Initial role grants are proposed, not verified against production role memberships. Review exact existing role restrictions and dynamic custom-role grants.
- Complete remaining indirect readers/writers in Cash, Cashier, Transfer, Warehouse, external mapping, reports, and shared bank-management UI; inspect related data returned in nested objects and payment side effects. Check received-check operations across Accountancy pages and all row-level dynamic action visibility.
- Check request linked record edits, workflow transitions, partial and full payments, account owner rules, deletion, approval, attachment ownership and count/search scoping with explicit user denies. Test all four scoped modes, live revocation in two tabs and open modals, and legacy roles.
- Development flags added but false; build and test with individual flags enabled after catalog setup. No production deployment, SQL execution or build performed.
