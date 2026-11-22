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

- `MenuService.OrganizeMenus` rearranges existing menu items and preserves roles; `GetMenusAsync` reads current role memberships.
- `menuNavigation.js` owns independent accordion state persisted in per-user `localStorage`; it does not open new groups on virtual-page navigation.
- `pageNavigation.js` calls `erpMenu.highlight(url)` instead of rebuilding the sidebar.
- Header search lists permitted menu links and matching settings-modal destinations with category paths.
- Favorite stars are on links and settings modal entries. Desktop stars appear on hover/focus; touch shows them continuously.
- `MenuController` validates antiforgery tokens and current permitted menu IDs for favorite/reorder requests.
- `MenuFavorites` rows are scoped by authenticated `UserId`; hidden/no-longer-allowed items remain in database, and are not rendered.
- `TaskHub` publishes `MenuAccessChanged` from account role editing. The already-open page is retained if its View permission remains; page-level operations can listen for `erp:permissions-changed`.
- Account deactivation is posted with antiforgery and admin authorization, revokes the account server-side and broadcasts `AccountDeactivated` to all live SignalR connections. A non-dismissible 3-second notice is shown and the client navigates to logout. The timer is presentation only; server checks the disabled flag on subsequent authentication requests.

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
10. Change a role during an active session. Verify SignalR updates menu and removes a revoked page. Check login and role access after reconnect.
11. Disable an account while logged in on multiple devices: show a non-dismissable message, 3-second countdown and server-enforced denial. Reactivate, verify full authentication is required. Test disconnect/offline fallback.
12. Confirm current workflows preserve unsaved form values when permissions change; pages with module-specific CUD authorization must attach to `erp:permissions-changed` to update their operation buttons.

## Integration limitations / follow-up

- A complete .NET Build, live browser UI testing, and applying the migration **were not performed** by the GitHub connector-based edit environment.
- The existing project does not expose a uniform client-side Create/Edit/Delete access contract for every page. The event `erp:permissions-changed` is emitted, but each page with action-specific access must reconcile its own buttons without destroying unsaved form content. **Do not consider all action-level live updates tested or completed yet.**
- The SignalR notice is best-effort when the device is connected. Account denial is enforced server-side even when the socket disconnects.
- Collapsed-sidebar flyouts and long-menu scrolling require browser QA across screen sizes.
