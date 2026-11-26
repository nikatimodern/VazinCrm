# ERP menu redesign — branch `menu`

This work is isolated from `main`. The previous navigation URLs remain available. The categorized sidebar reorganizes their entry points; it does not rename routes.

## Structure

- Pinned: Dashboard and Cartable
- User favorites (hidden until nonempty, first 5 with expand, reorder with Sortable)
- Daily work: Carizo, work reports
- Sales: Customers (`/Customer/Index?roleType=1`), Deals
- Supply: Suppliers (`/Customer/Index?roleType=2`), prices, supply invoices, amount requests
- Production, Warehouse & QC: production vouchers, warehouse vouchers, warehouse, QC
- Finance & Treasury: accountancy, received/issued checks, transfers, cashier and petty cash
- Management reports: cost and demands
- Settings: existing modal, headings added for its internal actions
- Fixed bottom: overdue deals, warnings, release notes and branding

The existing five mobile bottom navigation shortcuts are preserved; mobile drawer uses the categorized tree. Existing role restrictions of moved entries are retained when flattening the old nesting.

## Interaction and implementation

- `MenuService` evaluates the previous role and parent conditions on the original menu tree *before* reorganizing it. Role claims and effective visibility remain as in the pre-redesign menu.
- `menuNavigation.js` owns independent accordion state persisted in per-user `localStorage`; it does not open new groups on virtual-page navigation.
- `pageNavigation.js` calls `erpMenu.highlight(url)` instead of rebuilding the sidebar.
- Header search lists permitted menu links and matching settings-modal destinations with category paths.
- Favorite stars are on links and settings modal entries. Desktop stars appear on hover/focus; touch shows them continuously.
- `MenuController` validates antiforgery tokens and current permitted menu IDs for favorite/reorder requests.
- `MenuFavorites` rows are scoped by authenticated `UserId`; hidden/no-longer-allowed items remain in database, and are not rendered.

## Database migration (required)

Before deploying this branch, apply `20261122130000_AddMenuFavorites` to the **UserContextDb** database using the project's existing EF Core migration workflow. This migration creates `MenuFavorites(UserId, MenuId, SortOrder)` with composite primary key. Do not apply it to `CRMContextDb` by mistake.

## Acceptance tests before merge

1. Run a full `.NET` solution Build and database migration in staging.
2. As Admin, compare every original menu route to its new position, verify that customers/suppliers both include role type 3, and verify the settings modal links.
3. Compare SalesExpert, SalesAdmin, TaminExpert, TaminAdmin, FinancialExpert, FinancialAdmin, QCExpert, TanKhah and multi-role accounts. Unauthorized menus and favorites must not appear.
4. Navigate via Virtual DOM and browser Back/Forward. Sidebar must not reload, and no accordion must open automatically.
5. Expand several groups; refresh, logout and login on the same browser. Their open state should be retained per user without cross-user collisions.
6. Open search on desktop and mobile, verify Persian variations and settings-modal destinations, then select an item.
7. Add/remove favorites, reorder via drag/drop on desktop and mobile, exceed five favorites, and log in from another device. Revoke and then restore role access and check hidden favorites.
8. On small screens, verify that all five bottom shortcuts and the refreshed 'More' button still work.
9. In collapsed desktop mode, verify that flyouts are keyboard/touch accessible and neither hidden nor cut off.
10. Verify the same roles and multi-role combinations see exactly the same existing destinations as before.
11. Confirm that the original authentication and account-management workflows are unchanged.
12. Verify search, favorites, settings, and mobile navigation only list previously authorized links.

## Integration limitations / follow-up

- A complete .NET Build, live browser UI testing, and applying the migration **were not performed** by the GitHub connector-based edit environment.
- Entity-level authorization and live permission update integration are owned by the separate access-control work, not this branch.
- Collapsed-sidebar flyouts and long-menu scrolling require browser QA across screen sizes.

## Access-control isolation

This branch changes navigation presentation only. Its original authentication, role-management, OTP and SignalR handlers were restored from the branch's original `main` base. Menu filtering uses the original hierarchy and role claims before placing already-authorized entries into the new category layout. Do not use this branch as an implementation of new entity-based permissions.
